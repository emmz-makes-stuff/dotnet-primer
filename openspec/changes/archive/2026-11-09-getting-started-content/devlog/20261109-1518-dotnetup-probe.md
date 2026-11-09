# dotnetup probe (task 1.1)

<!-- Section: 1 · Tasks: 1.1 · Base: 5a2682c -->

Set up for this session:

- Emmz chose to create `change/getting-started-content` and commit the plan first (`5a2682c`). She
  also asked for `.mcp.json` to be gitignored, which is in that commit.
- Emmz asked for progress to go to the **panopticon** MCP (`report_progress`, change + section) at
  every section start and every task tick.

`dotnetup` 0.3.0-preview.1.26554.3, osx-arm64. Every mutating command was run with
`--install-path "<scratchpad>/dotnetup-root" --manifest-path "<scratchpad>/manifest.json"`. These
commands must run **unsandboxed**: the sandbox proxy refuses the download with `Error: Permission
denied (127.0.0.1:…)`. `env show` also errors in the sandbox ("home directory … is not writable"),
after printing its read-only part.

## Observed

- **The primer's `global.json` (`10.0.400`, `latestFeature`) becomes install spec `10.0`**, with the
  `global.json` path as its Source. A bare `dotnetup sdk install` from the repo root printed:
  `.NET SDK 10.0 will be installed since …/global.json specifies that version.`
  It resolved to **10.0.403**. So design D3's concern holds: a later `update` can move that spec to a
  10.0.5xx band, which `make deps` would reject. Keep the explicit `10.0.4xx` in the README and the
  setup check.
- `dotnetup sdk install 10.0.4xx` → 10.0.403. `dotnetup install lts` (top-level alias) → 10.0.403.
- `dotnetup runtime install aspnetcore@10.0` → ASP.NET Core runtime 10.0.14. The spec syntax is
  `component@version`, where the component is `runtime` or `aspnetcore`. A bare version means the core
  runtime.
- `dotnetup update`, with everything current, printed one line per spec
  (`.NET SDK 10.0.4xx is already up to date (10.0.403).`), then `Everything is up to date.`
- **Uninstall works by spec, and is reference-counted.**
  - `dotnetup sdk uninstall 10.0.4xx` printed "will **not** be uninstalled because it has other install
    specs that require it: .NET SDK 10.0 (source: …/global.json)". It only dereferenced the spec.
  - Removing a `global.json` spec needs `--source GlobalJson`, because the default is `Explicit`:
    `dotnetup sdk uninstall 10.0 --source GlobalJson`. Once the last spec went, the files were
    removed.
  - `runtime uninstall aspnetcore@10.0` removed no files while an SDK was installed, because the SDK
    ships the same 10.0.14 runtime.
  - Prompts are skipped with `--non-interactive`.
- `dotnetup list` columns are Component / Installed version / Install spec / Source. Source is
  `Command` or the `global.json` path. `--format Json` is available.
- `<root>/dotnet --version` from the repo root printed `10.0.403` with only the isolated root
  installed, so `global.json` roll-forward is satisfied.
- **Real root unchanged:** `dotnetup list` without the custom paths, diffed before vs after, printed
  `UNCHANGED`.

## Syntax notes for the page and cheatsheet

- `sdk install`, `install` and `runtime install` take several specs (`10.0.4xx 9.0.3xx`).
- Channels: `latest`, `lts`, `10`, `10.0`, `10.0.4xx`, or an exact `10.0.403`.
- `sdk update` and `runtime update` exist as well as the top-level `update`. `update` has
  `--update-global-json`.
- `env set <none|shell>`: this macOS build offers only `none` and `shell`. `everywhere` is Windows-only,
  per Learn.
- `init` and `env set` aren't run (design D2).

## Next

- Task 1.2: rewrite `site/src/content/docs/cheatsheets/dotnet-cli.md` (design D4). Run every `dotnet`
  command in a scratch folder, and the EF Core ones against a SQLite scratch project.
- Then 1.3: add the slug to `FULL_CONTENT_SLUGS`, then `make gates`.
