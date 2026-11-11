# dotnet-primer samples

Hands-on exercises that go with the [primer site](https://dotnet-primer.emmz.dev/). Each one is described on the site page for the area it practises.

These are **for practice only**. The tests are there so you can check your own progress. Nothing is recorded or submitted anywhere.

## Prerequisites

- **.NET SDK 10.0.4xx.** The repo's `global.json` pins it, so the samples use .NET 10 even if your default SDK is newer. Install it with [`dotnetup`](https://learn.microsoft.com/dotnet/core/tools/dotnetup/) from the repo root, then check that the repo selects it (it should print `10.0.4` and a patch number):

  ```sh
  dotnetup sdk install 10.0.4xx
  dotnet --version
  ```

  A bare `dotnetup sdk install` also works: it reads `global.json`. But it tracks the wider `10.0` channel, so a later `dotnetup update` could move you to a 10.0.5xx SDK. Naming `10.0.4xx` keeps you on the band the samples pin. New to `dotnetup`? [Getting started](https://dotnet-primer.emmz.dev/platform/dotnet-10-and-cli/) covers installing it, and what to do if you already have .NET from another installer.
- **Docker**: Rancher Desktop on Windows and macOS, or Docker Engine on Linux. [Docker](https://dotnet-primer.emmz.dev/platform/docker/) has the steps for each. Later exercises use it, through Testcontainers and container images. **Two exercises' tests need it.** Exercise 07's start SQL Server in a container with Testcontainers. The image is x64 only, so on an Apple silicon Mac see the Docker page's [macOS](https://dotnet-primer.emmz.dev/platform/docker/#macos) note. Exercise 12's tests build the app into two container images, one with `dotnet publish` and one from its Dockerfile, then run them and check what's inside. Their first run downloads the .NET base images. The others don't need Docker for their tests: 04–06 capture logs, spans and metrics in memory, and 08 to 11 keep their orders in memory. The Logging, Tracing and Metrics pages use Docker for one optional extra, the local dashboard that shows your app's telemetry.

## Layout

```
samples/
  Primer.slnx             # every project: starters, solutions, tests, benchmarks
  Primer.Solutions.slnx   # solutions and their tests only (used by the test gate)
  benchmarks/
    Benchmarks/           # BenchmarkDotNet: the Async & performance page's measurements
  exercises/
    01-configuration/
      Starter/            # your starting point, with TODO comments
      Solution/           # one way to finish it
      tests/              # the tests, shared by both test projects below
      Starter.Tests/      # runs the tests against Starter
      Solution.Tests/     # runs the same tests against Solution
    02-dependency-injection/   # same layout
    03-middleware/             # same layout
    04-logging/                # same layout
    05-tracing/                # same layout
    06-metrics/                # same layout
    07-ef-core/                # same layout; its tests need Docker
    08-grpc/                   # same layout, plus the published contract in tests/Protos/
    09-testing/                # a different shape: the app is written, and you write the tests
      App/                # the orders API to test; it's correct
      bugs/               # four seeded bugs, each a broken copy of one App file
      Starter.Tests/      # your tests go here, with a TODO for each technique
      Solution.Tests/     # one set of tests that catches every bug
      Checker/            # runs a test project against App, then against each bug
    10-security/               # same layout; tokens from the tests, or from `dotnet user-jwts`
    11-async/                  # same layout; the tests swap in a fake warehouse they control
    12-containers/             # same layout, plus a Dockerfile in each app; its tests need Docker
```

| Exercise | Practises | Described on |
| --- | --- | --- |
| `01-configuration` | binding configuration to validated options | [Configuration](https://dotnet-primer.emmz.dev/aspnet-core/configuration/) |
| `02-dependency-injection` | choosing service lifetimes | [Dependency Injection](https://dotnet-primer.emmz.dev/aspnet-core/dependency-injection/) |
| `03-middleware` | a correlation-id middleware, and pipeline order | [Middleware](https://dotnet-primer.emmz.dev/aspnet-core/middleware/) |
| `04-logging` | structured, source-generated logging at the right levels, with no personal data | [Logging](https://dotnet-primer.emmz.dev/observability/logging/) |
| `05-tracing` | custom spans with attributes, and recording a failure on a span | [Tracing](https://dotnet-primer.emmz.dev/observability/tracing/) |
| `06-metrics` | a counter and a histogram with units and low-cardinality tags | [Metrics](https://dotnet-primer.emmz.dev/observability/metrics/) |
| `07-ef-core` | a DbContext, a fluent model and its first migration, then saving, querying and change tracking | [EF Core](https://dotnet-primer.emmz.dev/data/ef-core/) |
| `08-grpc` | a `.proto` contract and a gRPC service: status codes, a server stream, and honouring deadlines and cancellation | [gRPC](https://dotnet-primer.emmz.dev/services/grpc/) |
| `09-testing` | writing the tests: unit tests, a test double, fake time, and `WebApplicationFactory` | [Testing](https://dotnet-primer.emmz.dev/testing/) |
| `10-security` | JWT bearer authentication from configuration, a fallback policy, and scope, role and resource-based authorization | [Security & auth](https://dotnet-primer.emmz.dev/security/) |
| `11-async` | async all the way with no blocking, passing cancellation down, background work that isn't lost, and independent calls run together | [Async & performance](https://dotnet-primer.emmz.dev/performance/async-and-performance/) |
| `12-containers` | an app that takes its port and settings from the environment, a non-root chiseled image from `dotnet publish`, and a multi-stage Dockerfile | [Containers](https://dotnet-primer.emmz.dev/deploy/containers/) |

## Working an exercise

Commands below run from the repo root.

1. Read the exercise on its site page (see the table above).
2. Run the tests against the starter. They fail at first, and the messages say what's missing. For exercise 01:

   ```sh
   dotnet test --project samples/exercises/01-configuration/Starter.Tests
   ```

3. Work through the `TODO` comments in `Starter/`. Re-run the tests until they're all green.
4. Compare your work with `Solution/`. There's usually more than one good answer.

To run the starter app itself:

```sh
dotnet run --project samples/exercises/01-configuration/Starter
```

### Exercise 09 works the other way round

The app is already written and correct, so there's nothing to fix in `App/`. You write the tests, in `Starter.Tests/`, working through its `TODO` comments. To see how good they are, run the checker:

```sh
dotnet run --project samples/exercises/09-testing/Checker
```

It runs your tests against the correct app first: they must all pass there. Then it runs them against each seeded bug, and reports which bugs your tests caught. For a bug they missed, it gives a hint that points at a `TODO`. When it reports all four caught, compare your tests with `Solution.Tests/`.

### Calling exercise 10 with a token

The tests make their own tokens. To call the running app yourself, create one with `dotnet user-jwts`, which comes with the SDK. Exercise 10 is already set up for it, so it changes no tracked file:

```sh
dotnet run --project samples/exercises/10-security/Starter

# in another terminal
dotnet user-jwts create --project samples/exercises/10-security/Starter --name ada --scope orders:write --output token
curl -H "Authorization: Bearer <token>" http://localhost:5290/catalogue
```

Add `--role support` for a support-staff token. The signing key goes to your user secrets, which the Starter and Solution share, so a token works against both.

## Running the benchmarks

`benchmarks/Benchmarks` holds the measurements on the [Async & performance](https://dotnet-primer.emmz.dev/performance/async-and-performance/) page: span parsing, string building, pooled buffers, source-generated JSON and frozen lookups. It isn't an exercise, and has no tests. Always run it in Release, because a Debug build isn't optimized and its numbers mean nothing:

```sh
dotnet run -c Release --project samples/benchmarks/Benchmarks -- --filter '*'
dotnet run -c Release --project samples/benchmarks/Benchmarks -- --filter '*Lookups*'
```

`--filter` picks benchmarks by name: `'*'` runs all five classes (about 6 minutes), and `'*Lookups*'` runs one. With no `--filter`, it asks which to run. The results go to the terminal and to `BenchmarkDotNet.Artifacts/` in the directory you ran it from, which Git ignores. Your numbers will differ from the page's. Compare the ratios.

## Gates (for maintainers)

```sh
dotnet build samples/Primer.slnx                          # all projects compile; warnings are errors
dotnet format samples/Primer.slnx --verify-no-changes     # formatting check
dotnet test --solution samples/Primer.Solutions.slnx      # solutions' tests must pass
dotnet run --project samples/exercises/09-testing/Checker -- Solution.Tests   # 09's solution tests must catch every seeded bug
```

Starters only have to compile. Their tests are meant to fail until the exercise is done. The benchmarks only have to compile too: the build gate builds them, and no gate runs them. Exercise 09's `Starter.Tests` has no tests, so the checker reports every bug as missed.
