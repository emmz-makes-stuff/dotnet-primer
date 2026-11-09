# Proposal

## Why

"Getting started" is the first page in the sidebar, and every exercise depends on it. Yet it is still
a stub, and so is its `dotnet` CLI cheatsheet, which ends in a "Coming soon" note. An engineer coming
from .NET 3.5 needs to get from nothing to a working .NET 10 toolchain, and to understand SDKs,
runtimes, `global.json`, SDK-style projects and the `dotnet` CLI, before the ASP.NET Core path makes
sense.

Emmz wants `dotnetup` to be the way readers install and manage SDKs and runtimes. It's a
cross-platform manager for user-level installations, and it's in public preview.

## What Changes

- **Write the full `platform/dotnet-10-and-cli` page**, replacing the stub. It covers:
  - **A short orientation for 3.5 engineers:** .NET Framework vs modern .NET, Windows-only vs
    cross-platform, the yearly release cadence with LTS and STS, and old vs SDK-style project files.
    The full mapping stays with the future "Coming from .NET 3.5" change.
  - **SDK vs runtime vs target framework.**
  - **Installing and managing SDKs and runtimes with `dotnetup`**, as the one install path the page
    teaches:
    - `dotnetup init`, `install`, `list`, `update` and `uninstall`;
    - channels such as `lts`, `10.0` and `10.0.4xx`;
    - access modes;
    - installing the SDK a repo's `global.json` asks for.

    A short caution says `dotnetup` is in preview, that production servers and containers get .NET in
    other ways, and links to Microsoft's other install methods.
  - **`global.json`**: SDK pinning and roll-forward.
  - **The SDK-style `.csproj`**: `TargetFramework`, implicit usings, nullable, `PackageReference`,
    `Directory.Build.props` and central package management.
  - **The `dotnet` CLI loop:** `new`, `build`, `run`, `watch`, `test`, `add package`/`reference`,
    `publish`, and solution files (`.slnx`).
  - **Pitfalls.**
  - **A "check your setup" recipe** that ends by running exercise 01's tests. There's no new exercise.
- **Complete the `dotnet` CLI cheatsheet**, and remove its "Coming soon" note.
  - Its SDKs section leads with `dotnetup`.
  - It adds the sections the stub promised: tools, user secrets, EF Core migrations and formatting.
- **Make `dotnetup` the install route** in `samples/README.md` prerequisites and in the `dotnet` row of
  `docs/dependencies.md`, dropping the brew cask alternative. `dotnetup` also gets its own row in
  `docs/dependencies.md`, with a matching `make deps` check, because the page's commands are checked by
  running them.
- **Gate:** add both slugs to `FULL_CONTENT_SLUGS`, so the site test fails if either page shows the
  placeholder again.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- **`topic-page-content`:**
  - "Full-content pages carry no placeholder" now also lists the Getting started page and the `dotnet`
    CLI cheatsheet.
  - Adds "Getting started page coverage", which includes `dotnetup` as the taught install path and its
    preview caution.
  - Adds "`dotnet` CLI cheatsheet coverage".
- **`sample-exercises`:** "Pinned .NET 10 SDK" now says the samples README tells engineers how to
  install the pinned SDK with `dotnetup`.

## Impact

- **Site:**
  - `site/src/content/docs/platform/dotnet-10-and-cli.md`;
  - `site/src/content/docs/cheatsheets/dotnet-cli.md`;
  - `site/tests/dist.test.ts` (the `FULL_CONTENT_SLUGS` entries).
- **Samples:** `samples/README.md` only. No code changes.
- **Docs and tooling:** `docs/dependencies.md` and the `deps` target in the `Makefile`.
  - The `dotnet` row's install column changes; its check stays the same.
  - There's a new `dotnetup` row and check.
- **Tooling:** `dotnetup` is already installed on the maintainer's machine (0.3.0-preview), so every
  `dotnetup` command shown can be run while writing. It's preview software, so command names and
  output may change. The page links to the Learn reference for that reason.
