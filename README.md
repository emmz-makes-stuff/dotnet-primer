# dotnet-primer

A primer for engineers moving from .NET Framework 3.5 to **.NET 10, ASP.NET Core, EF Core, gRPC and
containers**. It has a readable introduction, a learning path built around ASP.NET Core, a cheatsheet
for each area, links to Microsoft Learn throughout, and hands-on exercises you clone and run locally.

Live site: <https://dotnet-primer.emmz.dev/>. Found a mistake? Every page has a **Report a problem**
link that opens an issue here.

## Layout

| Path | What |
|---|---|
| `site/` | The static site (Astro + Starlight). Content is Markdown/MDX under `site/src/content/docs/`. See `site/README.md` for authoring. |
| `samples/` | Companion exercises (.NET 10, xUnit v3). Each exercise has a starter, a solution and shared tests. See `samples/README.md`. |
| `global.json` | Pins the .NET SDK to 10.0.4xx for everything in the repo. |
| `docs/` | Requirements (`DISCOVERY.md`), architecture decisions (`adrs/`), and tool dependencies (`dependencies.md`). |
| `openspec/` | Change tracking (OpenSpec). |

## Prerequisites

The tools are listed in [`docs/dependencies.md`](docs/dependencies.md): Node ≥22.23, .NET SDK 10.0.4xx,
and Docker for later exercises. To check your machine, run:

```sh
make deps
```

## Gates

Each gate prints `LABEL_EXIT:<n>`. To run them all, use `make gates`. The underlying commands are:

| Stack | Gate | Command |
|---|---|---|
| site | build | `npm --prefix site run build` |
| site | lint | `npm --prefix site run lint` |
| site | format | `npm --prefix site run format:check` |
| site | test | `npm --prefix site test` (needs the build first) |
| samples | build | `dotnet build samples/Primer.slnx` (analyzer warnings are errors, so this is also lint) |
| samples | format | `dotnet format samples/Primer.slnx --verify-no-changes` |
| samples | test | `dotnet test --solution samples/Primer.Solutions.slnx` |

Install the site's packages once with `npm ci --prefix site`. To preview the site locally, run
`npm --prefix site run preview` after a build.
