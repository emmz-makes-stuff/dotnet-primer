# Design

## Context

- **The stub** `testing/index.md` has a "What this covers" paragraph, two Learn links ("Testing in
  .NET" and "Integration tests in ASP.NET Core") and the "Coming soon" note. It's in
  `EXPECTED_SLUGS` and in the sidebar's Learning path. Neither it nor a cheatsheet is in
  `FULL_CONTENT_SLUGS` or `CHEATSHEETS`.
- **The samples already use what the page teaches:**
  - xUnit v3 on Microsoft.Testing.Platform, everywhere (ADR-0003, `global.json`);
  - `WebApplicationFactory<Program>` in 03 and 08;
  - Testcontainers in 07;
  - `FakeLogger` (`Microsoft.Extensions.Diagnostics.Testing`) in 04;
  - a hand-written double (`RecordingTracker`) in 08.

  The page can point at these as worked examples.
- **The exercise conventions** (`samples/README.md`, the `Exercise layout` requirement) assume that
  our tests check the engineer's code. Exercise 09 reverses that (proposal), so the layout, the
  "practice" requirement and the gate requirement change in this change's `sample-exercises` delta.
- **Decisions Emmz made** (2026-11-10): see proposal.md. In short:
  - seeded bugs, checked by a checker;
  - the orders domain;
  - fakes first, then NSubstitute;
  - code coverage as the extra (snapshot testing was dropped after §1.1; see D1);
  - one numbered `TODO` per technique in the Starter.
- **Current versions** (NuGet, 2026-11-10, confirmed in §1.1): `NSubstitute` 6.2.0,
  `NSubstitute.Analyzers.CSharp` 1.0.17, `Microsoft.Extensions.TimeProvider.Testing` 10.11.0 (the
  same line as the samples' `Diagnostics.Testing`), and `Microsoft.Testing.Extensions.CodeCoverage`
  18.12.0.

## Goals / Non-Goals

**Goals:**
- An engineer finishes the page able to choose between a unit test, a double, fake time and an
  integration test for a given behaviour, and to write each one.
- Exercise 09 gives real feedback on the engineer's tests, without anyone reading them.
- Exercise 09 can't silently become impossible: the gate proves that the Solution tests catch every
  seeded bug.
- Every command and output the page shows (filtering and coverage) comes from running it, not from
  memory.

**Non-Goals:**
- A general mutation-testing tool. The four bugs are written by hand, and the checker knows only
  them.
- Checking the quality of the engineer's tests beyond "do they pass on the correct API, and catch
  each bug".
- Changing the layout of exercises 01–08.

## Decisions

### D1. Page shape and order

The page follows the topic skeleton, with sections in this order:
1. **Outcome.**
2. **Cheatsheet tip.**
3. **Running tests:**
   - xUnit v3 on Microsoft.Testing.Platform, and the `"test": { "runner": ... }` setting in
     `global.json`;
   - `dotnet test --project` and `--solution`;
   - filtering, with the exact syntax confirmed in §1;
   - test projects as executables (`OutputType Exe`).
4. **Writing tests:**
   - `[Fact]` and `[Theory]` with `[InlineData]` / `[MemberData]`;
   - `Assert` (including `Assert.Throws` / `ThrowsAsync`);
   - async tests and `TestContext.Current.CancellationToken`;
   - naming that says the behaviour.
5. **Fixtures and lifetime:**
   - a new class instance per test;
   - the constructor, `IDisposable` and `IAsyncLifetime`;
   - `IClassFixture<T>`, collection fixtures and assembly fixtures.

   Parallelism is mentioned only as the reason collection fixtures exist (proposal: Out of scope).
6. **Test doubles:**
   - "do you need one?" (prefer the real thing when it's fast and deterministic);
   - a hand-written fake;
   - `FakeTimeProvider` with code that takes a `TimeProvider`;
   - `FakeLogger`;
   - NSubstitute (`Substitute.For<T>()`, `Returns`, `Received`, `DidNotReceive`, `Arg.Is`), with its
     analyzer;
   - one sentence on Moq.
7. **Integration tests:**
   - `WebApplicationFactory<Program>` and `public partial class Program`;
   - `CreateClient()`;
   - `ConfigureTestServices` to replace a registration;
   - `WithWebHostBuilder` for one test's variation;
   - environment and configuration overrides.
8. **Real dependencies:** Testcontainers in a few lines (a container in an assembly or class fixture
   via `IAsyncLifetime`), Docker as the prerequisite, and a link to exercise 07 and the Docker page.
9. **Code coverage:**
   - the coverage extension and its command, confirmed in §1;
   - the report format, and opening it;
   - "covered isn't tested": a line runs with no assertion on its result. This ties to exercise 09,
     where a test that only *calls* the API catches no bug.
10. **"Coming from .NET 3.5":** a short table (D8).
11. **Pitfalls,** then **Exercise 09**.

**No snapshot testing** (Emmz, 2026-11-10, after §1.1). Verify 33 fails the build without a
licence property: it has moved to a maintenance fee, which commercial users must pay. It was one
of the two extras, and Emmz dropped it rather than teach a paid tool or a hand-rolled substitute.

MSTest and NUnit today each get a sentence, in Running tests.

### D2. Exercise 09 layout

```
samples/exercises/09-testing/
  App/              Testing.App (Microsoft.NET.Sdk.Web): the correct orders API
  bugs/             one folder per seeded bug, each holding a replacement for one App file,
                    plus SeededBugs.targets (the switch, D4)
  Starter.Tests/    Testing.Starter.Tests: TODOs 1-4, no tests yet
  Solution.Tests/   Testing.Solution.Tests: the reference tests
  Checker/          Testing.Checker: a console app (D5)
  README.md
```

- **Two solution files:**
  - `Primer.slnx` gets all five projects;
  - `Primer.Solutions.slnx` gets `Testing.App` and `Testing.Solution.Tests` only. `dotnet test` over
    that solution runs the Solution tests against the correct API, as for the other exercises.
- **The checker isn't in `Primer.Solutions.slnx`**, so `dotnet test --solution` doesn't try to run a
  console app as tests. It's in `Primer.slnx`, so the build and format gates cover it.
- **There's no shared `tests/` folder:** the Starter and Solution test projects *are* the starter
  and the solution. Both reference `Testing.App`.

### D3. The orders API

In memory, money in integer cents as in 08. Every endpoint is a minimal API:

| Endpoint | Behaviour |
|---|---|
| `POST /orders` | Body: customer email and lines (SKU, quantity, unit price in cents). `201` with `Location`, id and total. `400` validation problem details for the same rules as 08 |
| `GET /orders/{id}` | `200` with the order, or `404` |
| `POST /orders/{id}/cancel` | `204` if cancelled, `409` if the cancel window has closed, `404` if unknown. A successful cancel tells the warehouse to release the order's lines |

**The rules, each in its own small file, so a bug replaces exactly one** (D4):
- `Pricing.cs`, static and pure: the subtotal, with **10% off when the subtotal is $100.00 (10,000
  cents) or more**. Computed in integer cents, with the discount rounded down.
- `CancelPolicy.cs`, static, `CanCancel(Order, TimeProvider)`: an order can be cancelled **up to
  and including 30 minutes after it was placed**. *Revised in §2:* it's a static method taking the
  `TimeProvider`, not a registered class with a primary constructor. Bug 3's copy ignores the
  parameter, and an unread primary-constructor parameter (CS9113) would fail the warnings-as-errors
  build. An unused method parameter doesn't.
- `OrderService.cs`: place and cancel. It takes `OrderStore`, `IWarehouse` and `TimeProvider`.
  Cancelling an already cancelled order is a `409`, like a closed window, so stock isn't released
  twice.
- `IWarehouse.cs`: `Task ReleaseAsync(int orderId, IReadOnlyList<OrderLine> lines, CancellationToken)`.
  The app's own implementation only logs: there's no real warehouse.
- `OrderRegistration.cs`: an `AddOrders()` extension that registers `OrderStore` as a singleton, the
  service (scoped), `IWarehouse` and `TimeProvider.System`. `Program.cs` calls it, maps the
  endpoints, and ends with `public partial class Program;`.

**The seeded bugs** (the requirement's four scenarios):

| # | Replaces | The bug | Caught by | TODO |
|---|---|---|---|---|
| 1 | `Pricing.cs` | the discount needs *more than* $100.00 (`>` for `>=`) | a unit test of `Pricing` at the threshold | 1 |
| 2 | `OrderService.cs` | a successful cancel calls `ReleaseAsync` with no lines, so no stock goes back | a test double for `IWarehouse`, checking the arguments | 2 |
| 3 | `CancelPolicy.cs` | the window is judged against `DateTimeOffset.UtcNow`, not the `TimeProvider` | `FakeTimeProvider` advanced past 30 minutes | 3 |
| 4 | `OrderRegistration.cs` | `OrderStore` is registered as scoped, so each request gets an empty store | `WebApplicationFactory`: place, then get | 4 |

Each bug file is a copy of the App file with **exactly one changed line**, which ends with a
`// SEEDED BUG n: …` comment. The checker's drift check relies on that (same line count, and the
only differing line carries the marker). *Revised in §2:* bug 2 can't just remove the
`ReleaseAsync` call, because the `async` method would then have no `await` (CS1998), so it releases
an empty list instead. The bugs are deliberately simple, with no trick or ambiguity, so a missed bug means a missing
test, not a misunderstanding.

**Rejected:** a bug in validation (`400`). A plain HTTP test would catch it as easily as a unit
test, so it wouldn't point at one technique.

### D4. Seeded bugs are separate builds, switched by an MSBuild property

`Testing.App.csproj` imports `../bugs/SeededBugs.targets`. When the `SeededBug` property is set
(`1`–`4`), the targets file removes the matching App file from `Compile` and includes that bug's
replacement. With no property, it does nothing. The App's C# has no `#if`, environment switch or
other test-only seam.

**Rejected:**
- **`#if SEEDED_BUG_n` in the App's code:** the code learners read would carry the bugs inline.
- **A runtime switch** (an environment variable picks a broken implementation in DI): simpler to
  run, but it puts test-only branches into the app, and it can't express bug 4 (the registration
  itself) without a switch inside `AddOrders()`.
- **One project per bug:** five near-identical `.csproj` files, and five more project references
  for the test projects to switch between.

### D5. The checker

`Testing.Checker` is a console app. `dotnet run --project Checker` checks `Starter.Tests` by
default, and a path argument checks another test project (the gate passes `Solution.Tests`). It:

1. runs `dotnet test --project <tests>` with no `SeededBug`;
2. if any test fails, prints the failed tests' names. It then says the tests disagree with the
   correct API, so the tests are wrong, and exits non-zero without trying the bugs;
3. if no tests ran, says so, points at TODO 1, and reports every bug missed;
4. otherwise, runs it once per bug with `-p:SeededBug=n`, **one at a time**, each with its own
   artifacts path (`--artifacts-path <dir>`), so the builds don't share `obj/`. The §1.1 probe
   showed that each run builds into `<dir>/{obj,bin}/<project>/debug`, and that the default build
   is unaffected afterwards;
5. prints one line per bug: caught, or missed with its hint. A hint names the TODO and the technique
   ("TODO 3: a test that controls the time"), not the test;
6. exits zero only when every bug is caught.

**Reading results:** it reads each run's exit code, using Microsoft.Testing.Platform's exit codes:
- 0: everything passed;
- 2: a test failed;
- 8: no tests ran.

Any other code (a failed build is 1) is reported as a build or run error, with the output shown.
For step 2's test names, it passes `--report-xunit-ctrf --report-xunit-ctrf-filename <name>
--results-directory <dir>` and reads the CTRF JSON. xUnit v3 writes it with no extra package. The
failed tests are the entries in `.results.tests[]` whose `status` is `failed`, and each one's `name`
is the full test name (§1.1).

**The checker runs on the pinned SDK only.** It shells out to `dotnet`, with the exercise folder as
its working directory, so `global.json` applies.

### D6. The gate

`make samples-test` and the `samples` CI workflow step each run two commands:
1. `dotnet test --solution samples/Primer.Solutions.slnx`, as today. This includes the Solution tests
   against the correct API.
2. `dotnet run --project samples/exercises/09-testing/Checker -- Solution.Tests`. It fails, naming
   the bug, if the Solution tests miss any seeded bug.

`SAMPLES_TEST_EXIT` is non-zero if either fails. That satisfies "Seeded bug becomes uncatchable".
CLAUDE.md's gate description for `make samples-test` is updated in the same section. The checker
adds 5 sequential builds and test runs to the gate. In the §1.1 probe, five cold runs took 10 s.

### D7. Starter and Solution tests

- **`Testing.Starter.Tests`:** one file per TODO, each holding a test class with no methods and a
  numbered comment. The comment says what to test and which tool to use, and points at the page
  section:
  1. `Pricing` (a plain unit test, theories welcome);
  2. cancelling tells the warehouse (a hand-written fake or NSubstitute);
  3. the cancel window (`FakeTimeProvider`);
  4. place then get through HTTP (`WebApplicationFactory`).

  The project references NSubstitute, `Microsoft.Extensions.TimeProvider.Testing` and
  `Microsoft.AspNetCore.Mvc.Testing`, so no TODO needs a package added.
- **`Testing.Solution.Tests`:** the same four files, written:
  - NSubstitute for `IWarehouse`, with `Received` and `DidNotReceive`;
  - `FakeTimeProvider`, injected through `ConfigureTestServices` in the integration test and passed
    directly in the unit tests;
  - a `WebApplicationFactory<Program>` subclass.

  The page's examples come from these files where they can.

### D8. The "Coming from .NET 3.5" table

Rows, each a .NET 3.5-era habit and its xUnit v3 equivalent:
- `[TestClass]` / `[TestFixture]`: a plain public class;
- `[TestMethod]` / `[Test]`: `[Fact]`;
- `[RowTest]` / `[TestCase]` (NUnit 2.5) and data-driven MSTest: `[Theory]` with data;
- `[ExpectedException]`: `Assert.Throws<T>(() => ...)` around the one call;
- `[TestInitialize]` / `[SetUp]`, and `[TestCleanup]` / `[TearDown]`: the constructor, and
  `Dispose` / `DisposeAsync`;
- `[ClassInitialize]` / `[TestFixtureSetUp]`: a class fixture;
- `DateTime.Now` in the code: inject `TimeProvider`;
- a static helper or singleton that can't be replaced: an interface registered in DI, and a fake;
- `mstest.exe` / `vstest.console`, `.testsettings`: `dotnet test`, test projects as executables;
- Rhino Mocks or TypeMock: hand-written fakes or NSubstitute.

### D9. Cheatsheet

It follows the cheatsheet shape (back link, `##` groups of copyable blocks, minimal prose):
- `dotnet test` commands (project, solution, filter, coverage);
- a test project's `.csproj` essentials;
- fact, theory, async test with the cancellation token, `Assert.Throws`;
- fixtures (`IClassFixture`, collection, assembly, `IAsyncLifetime`);
- doubles (a hand-written fake, `FakeTimeProvider`, `FakeLogger`, NSubstitute);
- `WebApplicationFactory` with `ConfigureTestServices`;
- Testcontainers in a fixture.

### D10. Verifying the page

As for EF Core and gRPC:
- every snippet compiles in a scratch project, with warnings as errors;
- the filter and coverage commands are run, and their real output is pasted;
- the snippets "from the exercise" are compared with the real files.

The coverage extension is used only in the scratch project (proposal: Impact).

## Risks / Trade-offs

- **The checker's builds take time** (five test runs, each a build): 10 s cold in the probe, perhaps
  more on CI. → They run one at a time with separate artifacts paths, the CI time is recorded in 3.4,
  and
  the checker prints progress so a learner doesn't think it has hung.
- **Bug files sit outside the App's default compile**, so the build gate doesn't compile them, and
  `dotnet format` doesn't see them. → The gate's checker run builds each bug with warnings as
  errors, so a bug that doesn't compile fails the gate. Formatting drift in the bug files is
  accepted: they're copies with a one-line change, and the checker diffs each against its App file
  (next risk).
- **A bug file can drift from its App file** after an edit to the App. → The checker checks it before
  running the bugs: each bug file must differ from its App file only in the lines its
  `// SEEDED BUG n:` comment marks. The check runs on every checker run, so it's part of the gate,
  and the reference tests that learners read stay free of it.
- **An engineer's test that passes by luck,** for example a time test that uses real time, might
  catch bug 3 only sometimes. → The checker reports per bug, and the hints name the right tool. Exact
  flakiness detection is out of scope.
- **Exercise 09 doesn't match the layout of 01–08.** → The README's layout section and the site's
  exercise description say so plainly, and the modified "Exercise layout" requirement makes it the
  defined exception.

## Open Questions

- The exact wording of the checker's hints, written in §2 and reviewed in the 5.x human check.
