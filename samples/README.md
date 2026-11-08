# dotnet-primer samples

Hands-on exercises that go with the [primer site](https://dotnet-primer.emmz.dev/). Each one is described on the site page for the area it practises.

These are **for practice only**. The tests are there so you can check your own progress. Nothing is recorded or submitted anywhere.

## Prerequisites

- **.NET SDK 10.0.4xx.** The repo's `global.json` pins it, so the samples use .NET 10 even if your default SDK is newer. Check with `dotnet --version` from the repo root. If it fails, install a .NET 10 SDK.
- **Docker** (OrbStack or Docker Desktop). Later exercises use it, through Testcontainers and container images. Exercise 01 doesn't need it.

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
```

## Working an exercise

Commands below run from the repo root.

1. Read the exercise on the site. Exercise 01 is on the [Configuration](https://dotnet-primer.emmz.dev/aspnet-core/configuration/) page.
2. Run the tests against the starter. They fail at first, and the messages say what's missing:

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
