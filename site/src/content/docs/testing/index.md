---
title: "Testing"
description: "Unit and integration tests with xUnit v3 on Microsoft.Testing.Platform: running and filtering tests, facts, theories and fixtures, fakes, fake time and NSubstitute, WebApplicationFactory, Testcontainers, code coverage, and coming from .NET 3.5."
learn:
  - title: "Testing in .NET"
    url: https://learn.microsoft.com/dotnet/core/testing/
  - title: "Microsoft.Testing.Platform overview"
    url: https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro
  - title: "Integration tests in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/test/integration-tests
  - title: "Use code coverage for unit testing"
    url: https://learn.microsoft.com/dotnet/core/testing/unit-testing-code-coverage
  - title: "TimeProvider and FakeTimeProvider"
    url: https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview
---

By the end of this page you'll be able to test a .NET app at the right level: run and filter tests with `dotnet test`, write facts and theories, share expensive setup with fixtures, replace a dependency with a fake, control the time in a test, send real HTTP requests to your app in memory, start a real database for the tests that need one, and read a coverage report without being fooled by it.

:::tip[Cheatsheet]
Keep the [Testing cheatsheet](/cheatsheets/testing/) open while you work.
:::

The examples test the orders API from [Exercise 09](#exercise-09-test-the-orders-api): it prices an order, with 10% off from $100.00, and lets the customer cancel within 30 minutes, which tells the warehouse to put the stock back.

## Running tests

The tests in this primer use **xUnit v3** on **Microsoft.Testing.Platform** (MTP), the test runner built into the .NET 10 SDK. A test project is an executable: it builds to an app that runs its own tests. `dotnet test` builds it and runs it. `global.json` at the repository root switches `dotnet test` to MTP:

```json title="global.json"
{
  "sdk": { "version": "10.0.400", "rollForward": "latestFeature" },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

```xml title="Orders.Tests.csproj"
<PropertyGroup>
  <OutputType>Exe</OutputType>
</PropertyGroup>
<ItemGroup>
  <Using Include="Xunit" />
  <PackageReference Include="xunit.v3" />
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="../App/Testing.App.csproj" />
</ItemGroup>
```

Run one project, or every test project in a solution:

```sh
dotnet test --project samples/exercises/09-testing/Solution.Tests
dotnet test --solution samples/Primer.Solutions.slnx
```

To run a subset, filter by class, method, namespace or trait. Names are full names, and `*` is a wildcard:

```sh
dotnet test --project Solution.Tests --filter-class TestingOrders.Tests.PricingTests
dotnet test --project Solution.Tests --filter-method "*Cancel*"
```

```txt
Test run summary: Passed!
  total: 4
  failed: 0
  succeeded: 4
  skipped: 0
```

The exit code says how the run went: `0` when every test passed, `2` when a test failed, and `8` when no tests ran, for example because a filter matched nothing. Exercise 09's checker relies on those codes.

MSTest and NUnit run on MTP too, and are fine choices. This primer uses xUnit v3, because most modern .NET open-source projects and docs examples do.

## Writing tests

A **fact** is a test with no inputs. A **theory** runs once per row of data. Give each test a name that says the behaviour it checks:

```csharp title="Solution.Tests/PricingTests.cs"
public class PricingTests
{
    [Fact]
    public void SubtotalIsQuantityTimesPriceForEveryLine()
    {
        OrderLine[] lines = [new("SKU-1", 2, 1_250), new("SKU-2", 1, 500)];

        Assert.Equal(3_000, Pricing.Subtotal(lines));
    }

    // The edges are where rules break: just below the threshold, exactly on it, and above it.
    [Theory]
    [InlineData(9_999, 9_999)]
    [InlineData(10_000, 9_000)]
    [InlineData(12_345, 11_111)]
    public void TotalTakesTenPercentOffAtOrAboveTheThreshold(long subtotalCents, long expectedTotalCents) =>
        Assert.Equal(expectedTotalCents, Pricing.Total(subtotalCents));
}
```

`[InlineData]` takes constants. For other data, such as objects or a list built in code, use `[MemberData(nameof(SomeProperty))]` with a static property of type `TheoryData<…>`.

Assert that a call throws by wrapping just that call: `Assert.Throws<T>(() => …)`, or `await Assert.ThrowsAsync<T>(() => …)` for async code. It returns the exception, so you can check its message.

**Async tests** return `Task`. Anything in the test that takes a `CancellationToken` gets the test's own, `TestContext.Current.CancellationToken`, so a cancelled or timed-out run stops the test instead of leaving it waiting. xUnit's analyzer reports a call without it (`xUnit1051`), and with warnings as errors, as in the samples, the build fails:

```csharp
[Fact]
public async Task CancellingReleasesTheOrdersLines()
{
    var ct = TestContext.Current.CancellationToken;
    var order = _orders.Place("ada@example.com", _lines);

    var outcome = await _orders.CancelAsync(order.Id, ct);

    Assert.Equal(CancelOutcome.Cancelled, outcome);
}
```

## Fixtures and lifetime

**xUnit creates a new instance of the test class for every test.** Fields set in the constructor are fresh each time, so tests can't leak state into each other through them. The constructor is the per-test setup, and `Dispose`, or `DisposeAsync` from `IAsyncLifetime`, is the teardown:

```csharp
public sealed class SeededStoreTests : IAsyncLifetime
{
    private readonly OrderStore _store = new();

    public ValueTask InitializeAsync()
    {
        _store.Add("ada@example.com", [new("SKU-1", 1, 500)], 500, DateTimeOffset.UtcNow);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

Some setup is too slow to repeat for every test: starting the app, or a database. A **fixture** is created once and handed to the tests that ask for it in their constructor:

- **Class fixture:** one instance for all the tests in one class. The class implements `IClassFixture<T>`.
- **Collection fixture:** one instance shared by several classes. A class marked `[CollectionDefinition("name")]` implements `ICollectionFixture<T>`, and each test class is marked `[Collection("name")]`. xUnit runs the test classes of one collection one after another, not in parallel, which is also what you want when they share state.
- **Assembly fixture:** one instance for the whole test run, declared with `[assembly: AssemblyFixture(typeof(T))]`. Exercise 07 starts its SQL Server this way.

```csharp
public sealed class OrdersApiTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    [Fact]
    public async Task UnknownOrderIsNotFound()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/orders/999", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

A fixture that implements `IAsyncLifetime` is started before its first test and disposed after its last one.

## Test doubles

A **test double** stands in for a dependency the test shouldn't use: one that's slow, not deterministic, has side effects, or isn't there at all. **First, check whether you need one.** If the real thing is fast and deterministic, like the in-memory `OrderStore`, use it: the test is then closer to production, and there's no double to keep in step with the real thing.

### A hand-written fake

When cancelling an order should tell the warehouse, the test needs to see what the warehouse was told. A fake that records its calls is a few lines:

```csharp
sealed class RecordingWarehouse : IWarehouse
{
    public List<(int OrderId, IReadOnlyList<OrderLine> Lines)> Released { get; } = [];

    public Task ReleaseAsync(int orderId, IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken)
    {
        Released.Add((orderId, lines));
        return Task.CompletedTask;
    }
}
```

```csharp
var warehouse = new RecordingWarehouse();
var orders = new OrderService(new OrderStore(), warehouse, new FakeTimeProvider());
var order = orders.Place("ada@example.com", _lines);

await orders.CancelAsync(order.Id, ct);

var released = Assert.Single(warehouse.Released);
Assert.Equal(order.Id, released.OrderId);
Assert.Equal(_lines, released.Lines);
```

Check the arguments, not just that a call happened. One of exercise 09's seeded bugs calls the warehouse with no lines at all.

### Fake time

Code that reads `DateTime.Now` or `DateTimeOffset.UtcNow` can't be tested at a chosen time, so a 30-minute rule would need a 30-minute test. Instead, the code takes a `TimeProvider`, which the app registers as `TimeProvider.System`:

```csharp title="App/CancelPolicy.cs"
public static bool CanCancel(Order order, TimeProvider time) => time.GetUtcNow() - order.PlacedAt <= Window;
```

The test passes a `FakeTimeProvider`, from the `Microsoft.Extensions.TimeProvider.Testing` package, and moves it forward:

```csharp title="Solution.Tests/CancelWindowTests.cs"
public class CancelWindowTests
{
    private static readonly DateTimeOffset _placedAt = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(_placedAt);
    private readonly Order _order = new(1, "ada@example.com", [new("SKU-1", 1, 500)], 500, _placedAt, OrderStatus.Placed);

    [Fact]
    public void OrderCanBeCancelledAtTheEndOfTheWindow()
    {
        _time.Advance(TimeSpan.FromMinutes(30));

        Assert.True(CancelPolicy.CanCancel(_order, _time));
    }

    [Fact]
    public void OrderCantBeCancelledAfterTheWindow()
    {
        _time.Advance(TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(1));

        Assert.False(CancelPolicy.CanCancel(_order, _time));
    }
}
```

When you advance it, `FakeTimeProvider` also fires the timers created from it, including `Task.Delay(delay, timeProvider)`. So code that waits can be tested without waiting, as long as it takes the `TimeProvider` for its delays too.

### Fake logging

To check what code logged, give it a `FakeLogger<T>`, from `Microsoft.Extensions.Diagnostics.Testing`. It keeps every entry, with its level, message and structured properties:

```csharp
var logger = new FakeLogger<LoggingWarehouse>();
var warehouse = new LoggingWarehouse(logger);

await warehouse.ReleaseAsync(42, _lines, ct);

Assert.Equal(LogLevel.Information, logger.LatestRecord.Level);
Assert.Equal("Released stock for order 42: 2 lines", logger.LatestRecord.Message);
```

[Exercise 04](/observability/logging/#exercise-04-logging-an-order-endpoint) checks its log entries this way, through the whole app.

### NSubstitute

Hand-written fakes get tedious for interfaces with many members, or when every test wants a different answer. **NSubstitute** creates a double for an interface at run time. `Returns` sets what a call answers, and `Received` and `DidNotReceive` check the calls made:

```csharp title="Solution.Tests/CancelReleasesStockTests.cs"
private readonly IWarehouse _warehouse = Substitute.For<IWarehouse>();

[Fact]
public async Task CancellingReleasesTheOrdersLines()
{
    var ct = TestContext.Current.CancellationToken;
    var order = _orders.Place("ada@example.com", _lines);

    await _orders.CancelAsync(order.Id, ct);

    await _warehouse.Received(1).ReleaseAsync(
        order.Id, Arg.Is<IReadOnlyList<OrderLine>>(lines => lines.SequenceEqual(_lines)), Arg.Any<CancellationToken>());
}

[Fact]
public async Task RefusedCancelReleasesNothing()
{
    var ct = TestContext.Current.CancellationToken;
    var order = _orders.Place("ada@example.com", _lines);
    _time.Advance(TimeSpan.FromHours(1));

    await _orders.CancelAsync(order.Id, ct);

    // WithAnyArgs: the arguments below are placeholders; no call at all may have been made.
    await _warehouse.DidNotReceiveWithAnyArgs().ReleaseAsync(default, default!, ct);
}
```

Add `NSubstitute.Analyzers.CSharp` too. It catches mistakes the compiler can't, such as setting up a member that isn't virtual and can't be substituted.

You'll also meet **Moq** in existing code: it does the same job, but many teams moved to NSubstitute after a 2023 release quietly collected developers' email hashes during the build.

## Integration tests

Unit tests can't see routing, model binding, status codes or how the app's services are registered. **`WebApplicationFactory<Program>`**, from `Microsoft.AspNetCore.Mvc.Testing`, starts the whole app in memory, with no port and no web server. `CreateClient()` gives an `HttpClient` that sends requests straight into it.

The factory needs to reach the app's `Program` class. With top-level statements, it's generated and internal, so the app's `Program.cs` ends with:

```csharp
public partial class Program;
```

To replace a service for the tests only, subclass the factory and use `ConfigureTestServices`. It runs after the app's own registrations, so what it adds wins:

```csharp title="Solution.Tests/OrdersApiTests.cs"
public sealed class OrdersApiFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));

    public IWarehouse Warehouse { get; } = Substitute.For<IWarehouse>();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
            services.RemoveAll<IWarehouse>();
            services.AddSingleton(Warehouse);
        });
}
```

```csharp
[Fact]
public async Task PlacedOrderCanBeReadBack()
{
    var ct = TestContext.Current.CancellationToken;

    var placed = await _client.PostAsJsonAsync("/orders", _valid, ct);
    Assert.Equal(HttpStatusCode.Created, placed.StatusCode);
    var created = await placed.Content.ReadFromJsonAsync<Order>(ct);

    // A second request: it only finds the order if every request shares the same store.
    var read = await _client.GetAsync(placed.Headers.Location, ct);

    Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    var order = await read.Content.ReadFromJsonAsync<Order>(ct);
    Assert.Equal(created!.Id, order!.Id);
}
```

For one test's variation, `factory.WithWebHostBuilder(b => …)` returns a new factory with extra configuration, such as `b.UseSetting("key", "value")` or `b.UseEnvironment("Testing")`. Each factory starts its own copy of the app, so reuse one through a class fixture when the tests don't need their own.

## Real dependencies

An in-memory fake can't tell you whether your SQL works. For the tests that need a real database or broker, **Testcontainers** starts one in Docker for the test run and removes it afterwards. Exercise 07 starts SQL Server once, as an assembly fixture. Here it is shortened, without its comments or its message for a missing Docker engine. `Build()` is where Testcontainers looks for Docker, so it goes in `InitializeAsync`, not a field initialiser:

```csharp title="07-ef-core/tests/SqlServerFixture.cs"
public sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    public string ConnectionString =>
        _container?.GetConnectionString() ?? throw new InvalidOperationException("The SQL Server container hasn't started.");

    public async ValueTask InitializeAsync()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04").Build();
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
```

These tests **need a running Docker engine**: see [Docker](/platform/docker/). Pin the image tag, not `latest`, so the tests behave the same from one run to the next. [Exercise 07](/data/ef-core/#exercise-07-a-database-for-the-orders-app) practises it.

## Code coverage

Code coverage measures which lines and branches your tests ran. Add the `Microsoft.Testing.Extensions.CodeCoverage` package to the test project, then:

```sh
dotnet test --project Solution.Tests --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
```

```txt
  In process file artifacts produced:
    - …/TestResults/coverage.cobertura.xml
Test run summary: Passed!
  total: 13
```

The Cobertura file is XML that CI systems and editors read. Its first element has the totals. For exercise 09's Solution tests, they're 81 of 117 lines (69%) and 32 of 46 branches:

```xml
<coverage line-rate="0.6923076923076923" branch-rate="0.6956521739130435" lines-covered="81" lines-valid="117" branches-covered="32" branches-valid="46" …>
```

**Covered isn't tested.** A line counts as covered when a test runs it, whether or not any assertion checks what it did. A test that places, reads and cancels orders through the API without asserting anything still covers every line it runs, and catches none of exercise 09's seeded bugs. Use coverage to find the code no test reaches, not as proof that what's reached is right. Exercise 09 measures tests the other way: by the bugs they catch.

## Coming from .NET 3.5

| In MSTest v1 or NUnit 2                              | In xUnit v3                                                        |
| ---------------------------------------------------- | ------------------------------------------------------------------ |
| `[TestClass]` or `[TestFixture]`                     | A plain public class                                               |
| `[TestMethod]` or `[Test]`                           | `[Fact]`                                                           |
| `[TestCase]` (NUnit 2.5), or `[DataSource]` (MSTest) | `[Theory]` with `[InlineData]` or `[MemberData]`                   |
| `[ExpectedException(typeof(T))]`                     | `Assert.Throws<T>(() => …)` around the one call that should throw  |
| `[TestInitialize]` or `[SetUp]`                      | The test class's constructor                                       |
| `[TestCleanup]` or `[TearDown]`                      | `Dispose`, or `DisposeAsync` from `IAsyncLifetime`                 |
| `[ClassInitialize]` or `[TestFixtureSetUp]`          | A class fixture: `IClassFixture<T>`                                |
| `DateTime.Now` in the code under test                | Inject `TimeProvider`, and use `FakeTimeProvider` in tests         |
| A static helper or singleton that can't be replaced  | An interface registered in DI, and a fake or a substitute in tests |
| Rhino Mocks or TypeMock                              | A hand-written fake, or NSubstitute                                |
| `mstest.exe`, `vstest.console`, `.testsettings`      | `dotnet test`, and test projects that are executables              |

## Pitfalls

- **Code under test reads `DateTime.Now`.** Time rules can't be tested at the edges, and tests fail at midnight or in another time zone. Fix: take a `TimeProvider`, and use `FakeTimeProvider` in tests.
- **State shared between tests through `static` fields.** Tests pass alone and fail together, or in a different order. Fix: instance fields, which xUnit creates fresh for each test, or a fixture for state you mean to share.
- **A new `WebApplicationFactory` for every test.** Every test starts the app again, and the run gets slow. Fix: share one through a class fixture, and use `WithWebHostBuilder` only for the tests that need different settings.
- **`Thread.Sleep` or `Task.Delay` to wait for something.** The test is slow, and still flaky on a busy machine. Fix: `FakeTimeProvider.Advance`, or await the thing itself.
- **Asserting only that a call happened.** The code calls the dependency with the wrong arguments, and the test still passes. Fix: check the arguments that matter.
- **Testing only far from the edges.** A `>` that should be `>=` passes every test except the one at the boundary. Fix: test just below, exactly on, and just above each threshold.
- **Treating coverage as proof.** Lines run without assertions count as covered. Fix: read coverage for what's missed, and check behaviour with assertions.
- **Error `xUnit1051`.** A call in a test takes a `CancellationToken`, and the test didn't pass one. Fix: pass `TestContext.Current.CancellationToken`.
- **`Zero tests ran`, exit code 8.** The filter matched nothing. Fix: filters take full names (`Namespace.Class.Method`), or a `*` wildcard.

## Exercise 09: test the orders API

**Goal:** this time **you write the tests**. The orders API in `App/` is already written, and it's correct: it prices an order, with 10% off from $100.00, lets the customer cancel within 30 minutes, and tells the warehouse to put the stock back. The exercise also has four **seeded bugs**: broken copies of the API, each wrong in one behaviour. Good tests catch each one. A **checker** runs your tests against the correct API, then against each bug, and tells you which bugs your tests caught.

**Where:** [`samples/exercises/09-testing`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/09-testing) in the GitHub repo. Clone the repo, then write your tests in the `Starter.Tests` project. **This exercise needs no Docker**: the API keeps its orders in memory, and the tests run it in process.

**Steps:** each `TODO` in `Starter.Tests` is one technique from this page, and one seeded bug.

1. In `PricingTests.cs`, test `Pricing` with plain unit tests (TODO 1).
2. In `CancelReleasesStockTests.cs`, test that cancelling tells the warehouse to release the order's lines, with a hand-written fake or NSubstitute (TODO 2).
3. In `CancelWindowTests.cs`, test the 30-minute cancel window with a `FakeTimeProvider` (TODO 3).
4. In `OrdersApiTests.cs`, test the API through HTTP with `WebApplicationFactory<Program>` (TODO 4).

**Check your work:** run the checker, from the repository root:

```sh
dotnet run --project samples/exercises/09-testing/Checker
```

It checks `Starter.Tests`. Before you write anything, it reports every bug missed. Your tests must pass against the correct API first: if one fails there, the test is wrong, and the checker names it. Then each bug is reported as caught, or missed with a hint that points at its `TODO`:

```txt
Against the correct API ... all your tests pass.
Against bug 1 (the discount) ... caught, by PricingTests.TotalTakesTenPercentOffAtOrAboveTheThreshold(subtotalCents: 10000, expectedTotalCents: 9000).
Against bug 2 (the warehouse) ... missed: every test still passes.
  Hint: TODO 2: a test double for IWarehouse. Check what the warehouse was told, not only that it was called.
```

It takes a few seconds per bug: each one is a separate build.

Stuck? Compare with the `Solution.Tests` project next to it, which catches all four. The exercises are for practice only, and nothing is recorded or submitted.
