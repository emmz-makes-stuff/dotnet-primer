# Design

## Context

- **The two stubs:**
  - `platform/dotnet-10-and-cli.md` has a "What this covers" paragraph and the "Coming soon" note.
  - `cheatsheets/dotnet-cli.md` has SDKs, Create, Build/run/test, Packages and Publish blocks, and ends
    in a "Coming soon" note promising tools, user secrets, EF Core migrations and formatting.
  - The cheatsheet ↔ page link pair is already in the `CHEATSHEETS` map in `site/tests/dist.test.ts`.
    Neither page is in `FULL_CONTENT_SLUGS`.
- **The conventions are set.** `site/README.md` holds the topic page skeleton, the cheatsheet shape, the
  code-sample rules and the "add a finished page to `FULL_CONTENT_SLUGS`" step, all from
  `aspnet-core-foundations-content`.
- **`dotnetup` 0.3.0-preview is installed** on the maintainer's machine, with access mode `shell`.
  - `dotnetup list` shows a managed root holding 10.0.403, 10.0.400, 11.0 RC and older SDKs.
  - Another repo's `global.json` showed up as install spec `10.0`. So `dotnetup` turns a `global.json`
    pin into a channel, not an exact version.
- **The samples pin:** `global.json` is `10.0.400` with `rollForward: latestFeature`, and
  `make deps` checks `dotnet --list-sdks | grep '^10\.0\.4'`.
- **Learn's guidance:** `dotnetup` is in public preview. Learn recommends the installers or a package
  manager for production machines and Visual Studio users. It also warns that Windows `everywhere`
  mode overrides machine-wide installs.

## Goals / Non-Goals

**Goals:**
- The page takes a reader with no .NET on the machine to a green exercise 01 run, using `dotnetup`.
- Every `dotnetup` and `dotnet` command shown has been run, and every output or behaviour described
  has been seen.
- The cheatsheet is complete, with no placeholder.

**Non-Goals:**
- The full .NET 3.5 → 10 mapping. That's the future "Coming from .NET 3.5" change.
- A new exercise. The setup check reuses exercise 01.
- Teaching Visual Studio, Rider or VS Code setup. The page is about the CLI.
- Production or container installation. The page links out for that, and the deploy pages cover
  containers.

## Decisions

### D1. Page shape: the topic skeleton, with a setup check in place of the exercise

The page follows the topic skeleton in `site/README.md`:
1. the outcome statement and a cheatsheet tip;
2. the concepts;
3. `## Pitfalls`;
4. `## Check your setup`, which sits where an exercise would and is the same kind of block: a goal, then
   steps, then a command to run.

The sections, in order:
1. **Modern .NET, if you're coming from 3.5.** Framework vs .NET; the yearly November release, with
   LTS in even years and STS in odd ones, and why the primer uses .NET 10 LTS; cross-platform; and a
   short note that `.csproj` files look different. It links forward to the Coming from 3.5 section.
2. **SDK, runtime and target framework.** What each is. One SDK builds for many targets. The ASP.NET
   Core runtime vs the base runtime.
3. **Install .NET with `dotnetup`.**
   - The preview caution comes first, as a `:::caution` aside with links to Learn's install pages for
     Windows, macOS and Linux.
   - Then:
     - getting `dotnetup` (Learn's getting-started script);
     - `dotnetup init`;
     - channels (`lts`, `10.0`, `10.0.4xx`, exact);
     - access modes (`shell`/`none`, and why Windows `everywhere` needs care);
     - `dotnetup sdk install` with no channel, which reads `global.json`;
     - runtimes (`dotnetup runtime install aspnetcore@10.0`);
     - `list`, `update` and `uninstall`.
4. **Pin the SDK with `global.json`.** `version`, `rollForward` (with `latestFeature` as used here) and
   `dotnet --version` from inside the repo.
5. **The SDK-style project file.** Annotated from the primer's own projects: `Sdk="Microsoft.NET.Sdk.Web"`,
   `TargetFramework`, `ImplicitUsings`, `Nullable`, `PackageReference` without versions, then
   `Directory.Build.props` and `Directory.Packages.props` (central package management).
6. **The `dotnet` CLI loop.** `new`, `sln`/`.slnx`, `build`, `run`, `watch`, `test`, `add package`,
   `add reference` and `publish` (framework-dependent, self-contained, and the container target in one
   line, linking forward to Containers).
7. **Pitfalls,** for example:
   - a `global.json` asking for an SDK that isn't installed;
   - running from the wrong folder, so `global.json` isn't seen;
   - `dotnetup` access mode `none`, then `dotnet` isn't on `PATH`;
   - Windows `everywhere` hiding Visual Studio's SDKs;
   - `PackageReference` with a `Version` under central package management;
   - assuming `bin/` output is what you deploy (use `publish`).
8. **Check your setup:** clone the repo, run `dotnetup sdk install 10.0.4xx`, then
   `dotnet --version`, then `dotnet test --project samples/exercises/01-configuration/Solution.Tests`.

The format stays `.md`, because no component is needed.

### D2. dotnetup commands: verify in an isolated root, never the maintainer's own

Every `dotnetup` command that installs, updates or removes something is run with
`--install-path "<scratchpad>/dotnetup-root"` and `--manifest-path "<scratchpad>/manifest.json"`, so
the maintainer's real managed root and manifest are never changed. Read-only commands (`list`,
`--info`, `env show`, `--help`) run normally.

`init` and `env set` change shell profiles, so they aren't run. Their documented prompts and output
are checked against Learn instead, and the page shows them as Learn does, with `<resolved-version>`
placeholders, not invented version numbers.

The page links to the Learn `dotnetup` reference and says that preview commands can change.

### D3. Samples README and dependencies name the band explicitly

`dotnetup` turns a `global.json` pin into a channel (see Context). For the primer's
`10.0.400`/`latestFeature` pin, that channel might be `10.0`, which could later install a 10.0.5xx SDK
that the `make deps` check (`^10\.0\.4`) rejects. So:

- `samples/README.md` prerequisites tell engineers to run `dotnetup sdk install 10.0.4xx` from the repo
  root, then `dotnet --version`. They also mention that `dotnetup sdk install` with no channel reads
  `global.json`.
- **`docs/dependencies.md`, `dotnet` row:** the install column becomes `dotnetup sdk install 10.0.4xx`,
  the brew cask goes, and the check is unchanged.
- **New `dotnetup` row:** Why: "install/manage SDKs; verify the Getting started page". Version: any
  0.x preview. Check: `dotnetup --version`. Install: Learn's getting-started script. A matching line
  goes in the `make deps` recipe (`command -v dotnetup … || MISSING dotnetup — see docs/dependencies.md`),
  and the `dotnet` row's MISSING message drops brew.

The page itself teaches the bare `dotnetup sdk install` (from `global.json`) as the general rule. The
setup check uses the explicit band. Task 1.1 probes what spec the primer's own `global.json` produces,
and the page describes what was observed.

### D4. Cheatsheet: complete the sections and lead with dotnetup

The cheatsheet keeps its existing shape: copyable `sh` blocks with trailing comments, and the
"Back to" link.

- **SDKs** becomes **Install and select SDKs**: `dotnetup install lts|10.0|10.0.4xx`,
  `dotnetup sdk install` (from `global.json`), `dotnetup runtime install aspnetcore@10.0`,
  `dotnetup list`, `dotnetup update`, `dotnetup sdk uninstall …`, then `dotnet --version`,
  `--list-sdks` and `new globaljson`.
- **New sections:**
  - **Tools:** local tool manifest, `tool install`, `tool restore`, `tool run`, global tools.
  - **User secrets:** `init`, `set`, `list`, `remove`, linking to the Configuration page.
  - **EF Core migrations:** `dotnet tool install dotnet-ef`, `migrations add`, `database update`,
    `migrations script`.
  - **Formatting:** `dotnet format`, `--verify-no-changes`.
  - **Publish:** gains `--self-contained -r` and a framework-dependent line.
- The "Coming soon" note goes.

EF Core commands are checked against a scratch project with the SQLite provider, because no database
server is needed. The commands shown are provider-independent.

### D5. Code accuracy

As in the previous change:
- every `dotnet` command is run in a scratch folder on .NET 10.0.4xx;
- every project-file snippet is a real file that builds with `TreatWarningsAsErrors`;
- Learn links are checked with the link script.

### D6. Gate additions

- `FULL_CONTENT_SLUGS` gains `platform/dotnet-10-and-cli` and `cheatsheets/dotnet-cli`.
- The `CHEATSHEETS` pair already exists.
- There are no new tests. The spec's scenarios are covered by the placeholder test, the existing
  link tests, and the human check.

### D7. Section order

1. Cheatsheet (smaller; settles the command list the page then explains).
2. Page.
3. Samples README, `docs/dependencies.md` and `make deps`.
4. Human check: light and dark, the caution aside, copy buttons, search for "dotnetup", and a run of
   the README's setup steps.

## Risks / Trade-offs

- **`dotnetup` is preview software.** Commands or output may change after the page ships. This is
  mitigated by the caution aside, the link to the Learn reference, and showing placeholder versions
  rather than exact output.
- **The `global.json` to channel mapping is unverified for this repo's pin.** That's mitigated by D3
  (the explicit `10.0.4xx` in the README and setup check) and by task 1.1's probe. If the probe
  shows something surprising, the page describes what it observed, and the surprise goes to Emmz if it
  changes advice.
- **Windows behaviour can't be observed here.** The maintainer's machine is macOS, so Windows-only
  claims (`everywhere` mode, elevation) come from Learn and are worded as Learn states them.
- **Readers who already have .NET from the installer** may end up with two installs. The page has a
  pitfall on this, and points to `--migrate-from-system` as Learn documents it.
