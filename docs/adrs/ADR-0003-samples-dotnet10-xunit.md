# ADR-0003: Companion samples on .NET 10 with xUnit v3, in this repo

- **Status:** Accepted
- **Date:** 2026-11-08
- **Deciders:** Emmz

## Context

`docs/DISCOVERY.md` puts in scope hands-on exercises described on the site, with starter and solution
projects that engineers clone and run locally. They are kept **in this repo** and are **for practice
only**. The subject is .NET 10, so the samples must target it. Machines may have a newer default SDK
(Emmz's is the 11 RC).

## Decision

- **Location:** `samples/`.
- **SDK pin:** a root `global.json` pins SDK **10.0.4xx** (`rollForward: latestFeature`) and sets the
  test runner to **Microsoft.Testing.Platform**.
- **Target framework:** `net10.0`.
- **Tests:** **xUnit v3**. The site's Testing chapter teaches the same framework.
- **Two solution files:**
  - `samples/Primer.slnx`: every starter, solution and test project;
  - `samples/Primer.Solutions.slnx`: solution projects and their tests only.
- **Gate policy:** every starter and solution must **compile**. Tests run **only** against the
  solutions, so starters may have TODOs whose tests would fail.
- **Strictness:** `samples/Directory.Build.props` enables nullable, `TreatWarningsAsErrors`, and the
  .NET analyzers. That makes the build the lint gate.
- **Docker:** sample tests may use Testcontainers, so `docker` is a dependency of the samples test
  gate.

## Gate commands

| Gate | Command |
|---|---|
| build | `dotnet build samples/Primer.slnx` |
| test | `dotnet test --solution samples/Primer.Solutions.slnx` |
| format | `dotnet format samples/Primer.slnx --verify-no-changes` |
| lint | none separate (analyzer warnings fail the build) |

## Alternatives rejected

- **MSTest / NUnit:** both viable. xUnit v3 is the most common choice in modern .NET OSS and in docs
  examples.
- **Starters fully green:** stricter, but harder to author, and it limits what an exercise can leave
  as TODO.
- **Separate samples repo:** Emmz chose to keep everything in one repo.
