---
title: Testing cheatsheet
description: dotnet test commands, xUnit v3 test shapes and fixtures, fakes and NSubstitute, fake time and logging, WebApplicationFactory, Testcontainers and code coverage, on one page.
prev: false
next: false
learn:
  - title: Testing in .NET
    url: https://learn.microsoft.com/dotnet/core/testing/
  - title: Integration tests in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/test/integration-tests
  - title: Microsoft.Testing.Platform overview
    url: https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro
---

Back to [Testing](/testing/).

## Run tests

```sh
dotnet test --project tests/Shop.Tests                           # one test project
dotnet test --solution Shop.slnx                                 # every test project in a solution
dotnet test --project tests/Shop.Tests --filter-class Shop.Tests.PricingTests
dotnet test --project tests/Shop.Tests --filter-method "*Discount*"
dotnet test --project tests/Shop.Tests --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
```

```json title="global.json"
{
  "sdk": { "version": "10.0.400", "rollForward": "latestFeature" },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

Exit codes: `0` all passed, `2` a test failed, `8` no tests ran, `1` the build failed.

## Test project

```xml title="Shop.Tests.csproj"
<PropertyGroup>
  <OutputType>Exe</OutputType>                      <!-- a test project is an app you can run -->
</PropertyGroup>
<ItemGroup>
  <Using Include="Xunit" />
  <PackageReference Include="xunit.v3" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />            <!-- WebApplicationFactory -->
  <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />   <!-- FakeTimeProvider -->
  <PackageReference Include="Microsoft.Extensions.Diagnostics.Testing" />    <!-- FakeLogger -->
  <PackageReference Include="NSubstitute" />
  <PackageReference Include="NSubstitute.Analyzers.CSharp" PrivateAssets="all" />
  <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" />   <!-- dotnet test --coverage -->
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="../../src/Shop/Shop.csproj" />
</ItemGroup>
```

## Facts and theories

```csharp
public class PricingTests
{
    private readonly OrderService _orders = new(new OrderStore(), Substitute.For<IWarehouse>(), TimeProvider.System);

    [Fact]
    public void SubtotalAddsEveryLine() =>
        Assert.Equal(3_000, Pricing.Subtotal([new("SKU-1", 2, 1_250), new("SKU-2", 1, 500)]));

    [Theory]
    [InlineData(9_999, 9_999)]
    [InlineData(10_000, 9_000)]
    public void TotalTakesTenPercentOffFrom100Dollars(long subtotalCents, long totalCents) =>
        Assert.Equal(totalCents, Pricing.Total(subtotalCents));

    [Fact]
    public void NegativeQuantityThrows() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderLine("SKU-1", -1, 500).Validate());

    [Fact]
    public async Task CancelReturnsTheOutcome()
    {
        var ct = TestContext.Current.CancellationToken;   // pass it to everything that takes one
        var outcome = await _orders.CancelAsync(42, ct);
        Assert.Equal(CancelOutcome.NotFound, outcome);
    }
}
```

## Setup, teardown and fixtures

```csharp
// A new instance of the class for every test: the constructor is the setup, Dispose the teardown.
public sealed class OrderServiceTests : IDisposable
{
    private readonly OrderStore _store = new();
    public void Dispose() { /* clean up */ }
}

// Async setup and teardown.
public sealed class SeededTests : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await SeedAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// One instance for every test in the class.
public sealed class ApiTests(ShopFactory factory) : IClassFixture<ShopFactory>
{
    [Fact]
    public void AppStarts() => Assert.NotNull(factory.Services);
}

// One instance shared by several classes. They run one after another, not in parallel.
[CollectionDefinition("database")]
public sealed class SharedDatabase : ICollectionFixture<DatabaseFixture>;

[Collection("database")]
public sealed class OrderQueryTests(DatabaseFixture db)
{
    [Fact]
    public void HasADatabase() => Assert.NotEmpty(db.ConnectionString);
}
```

```csharp
// One instance for the whole test run: at the top of any file, before its types.
// Any test class can take it in its constructor.
[assembly: AssemblyFixture(typeof(DatabaseFixture))]
```

## Test doubles

```csharp
// A hand-written fake: records its calls.
sealed class RecordingWarehouse : IWarehouse
{
    public List<int> Released { get; } = [];
    public Task ReleaseAsync(int orderId, IReadOnlyList<OrderLine> lines, CancellationToken ct)
    {
        Released.Add(orderId);
        return Task.CompletedTask;
    }
}
```

```csharp
// Fake time (Microsoft.Extensions.Time.Testing): the code under test takes a TimeProvider.
var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));
time.Advance(TimeSpan.FromMinutes(31));

// Fake logging (Microsoft.Extensions.Logging.Testing).
var logger = new FakeLogger<OrderService>();
Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);

// NSubstitute.
var warehouse = Substitute.For<IWarehouse>();
var pricing = Substitute.For<IPriceList>();
pricing.PriceOf("SKU-1").Returns(1_250);
await warehouse.Received(1).ReleaseAsync(42, Arg.Is<IReadOnlyList<OrderLine>>(l => l.Count == 2), Arg.Any<CancellationToken>());
await warehouse.DidNotReceiveWithAnyArgs().ReleaseAsync(default, default!, ct);
```

## Integration tests

```csharp
// The app needs a visible Program class: add this line to the end of its Program.cs.
public partial class Program;

public sealed class ShopFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider Time { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();               // replace a registration
            services.AddSingleton<TimeProvider>(Time);
        });
    }
}

public sealed class OrdersApiTests(ShopFactory factory) : IClassFixture<ShopFactory>
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

```csharp
// One test's variation, without a new factory class.
using var custom = factory.WithWebHostBuilder(b => b.UseSetting("Orders:CancelWindowMinutes", "5"));
```

## Testcontainers

```csharp
// Needs Docker. One SQL Server for the whole run (Testcontainers.MsSql).
public sealed class DatabaseFixture : IAsyncLifetime
{
    private MsSqlContainer? _sql;

    public string ConnectionString => _sql?.GetConnectionString() ?? throw new InvalidOperationException("Not started.");

    public async ValueTask InitializeAsync()
    {
        _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04").Build();   // looks for Docker
        await _sql.StartAsync();
    }

    public ValueTask DisposeAsync() => _sql?.DisposeAsync() ?? ValueTask.CompletedTask;
}
```
