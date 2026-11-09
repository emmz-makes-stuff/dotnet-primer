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
- **Docker** (OrbStack or Docker Desktop). Later exercises use it, through Testcontainers and container images. Exercises 01–03 don't need it.

## Layout

```
samples/
  Primer.slnx             # every project: starters, solutions, tests
  Primer.Solutions.slnx   # solutions and their tests only (used by the test gate)
  exercises/
    01-configuration/
      Starter/            # your starting point, with TODO comments
      Solution/           # one way to finish it
      tests/              # the tests, shared by both test projects below
      Starter.Tests/      # runs the tests against Starter
      Solution.Tests/     # runs the same tests against Solution
    02-dependency-injection/   # same layout
    03-middleware/             # same layout
```

| Exercise | Practises | Described on |
| --- | --- | --- |
| `01-configuration` | binding configuration to validated options | [Configuration](https://dotnet-primer.emmz.dev/aspnet-core/configuration/) |
| `02-dependency-injection` | choosing service lifetimes | [Dependency Injection](https://dotnet-primer.emmz.dev/aspnet-core/dependency-injection/) |
| `03-middleware` | a correlation-id middleware, and pipeline order | [Middleware](https://dotnet-primer.emmz.dev/aspnet-core/middleware/) |

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

## Gates (for maintainers)

```sh
dotnet build samples/Primer.slnx                          # all projects compile; warnings are errors
dotnet format samples/Primer.slnx --verify-no-changes     # formatting check
dotnet test --solution samples/Primer.Solutions.slnx      # solutions' tests must pass
```

Starters only have to compile. Their tests are meant to fail until the exercise is done.
