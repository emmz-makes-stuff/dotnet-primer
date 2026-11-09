# Tasks

## 1. dotnetup probe and the dotnet CLI cheatsheet

- [x] 1.1 Probe `dotnetup` in an isolated root (design D2). Use
  `--install-path "<scratchpad>/dotnetup-root"` and `--manifest-path "<scratchpad>/manifest.json"`.
  - Run:
    - `sdk install 10.0.4xx`;
    - a bare `sdk install` from the repo root;
    - `runtime install aspnetcore@10.0`;
    - `list`, `update` and `sdk uninstall`.
  - Record in a devlog note:
    - which install spec the primer's `global.json` (`10.0.400`, `latestFeature`) turns into;
    - which version it resolves to;
    - the exact command syntax that worked.
  - Verify: `dotnetup list` without the custom paths is unchanged from before the probe.
- [x] 1.2 Rewrite `site/src/content/docs/cheatsheets/dotnet-cli.md` (design D4).
  - The Install and select SDKs section leads with `dotnetup`.
  - Add tools, user secrets, EF Core migrations, formatting and the extra publish lines.
  - Remove the "Coming soon" note and keep the "Back to" link.
  - Verify: every command has been run in a scratch folder on 10.0.4xx, with the `dotnetup` ones done
    as in 1.1, and the EF Core ones against a SQLite scratch project.
- [x] 1.3 Add `cheatsheets/dotnet-cli` to `FULL_CONTENT_SLUGS`. Verify that `make gates` shows
  `GATES_EXIT:0`.

## 2. The .NET 10 and dotnet CLI page

- [x] 2.1 Rewrite `site/src/content/docs/platform/dotnet-10-and-cli.md` sections 1–6 per design D1.
  These are the orientation, SDK/runtime/target, Install with `dotnetup` (`:::caution` preview aside
  first, with Learn links for Windows, macOS and Linux), `global.json`, the SDK-style project file, and
  the CLI loop. Keep the frontmatter Learn links and add the `dotnetup` overview.
  - Verify:
    - the page describes the `global.json` behaviour 1.1 observed;
    - the project-file snippets are copies of real files that build with warnings as errors;
    - the `init`/`env` output matches Learn, with `<resolved-version>` placeholders.
- [x] 2.2 Add `## Pitfalls` and `## Check your setup` (design D1, items 7–8). Verify the setup check
  by following it exactly in a fresh clone in the scratchpad: it ends with exercise 01's solution
  tests passing.
- [x] 2.3 Add `platform/dotnet-10-and-cli` to `FULL_CONTENT_SLUGS`, and check every Learn link on the
  page with the link script. Verify: the link check passes and `make gates` shows `GATES_EXIT:0`.

## 3. Samples README and dependencies

- [ ] 3.1 Update `samples/README.md` prerequisites (design D3). Run `dotnetup sdk install 10.0.4xx`
  from the repo root, then `dotnet --version`, with a note that a bare `dotnetup sdk install` reads
  `global.json`, and a link to the Getting started page. Verify: the steps match what 1.1 observed.
- [ ] 3.2 Update `docs/dependencies.md` and the `Makefile` `deps` recipe (design D3).
  - In the `dotnet` row, `dotnetup` becomes the install route.
  - Add a new `dotnetup` row and check.
  - Remove brew from the MISSING message.
  - Write a devlog note on why the dependency is added, as CLAUDE.md requires.
  - Verify that `make deps` shows `DEPS_EXIT:0`, and that a temporarily misspelled check prints the
    MISSING line. Revert that afterwards.

## 4. Human check

- [ ] 4.1 Human check (stop and ask). Emmz previews the site with
  `npm --prefix site run build && npm --prefix site run preview`, and checks:
  - both pages in light and dark modes, including the caution aside;
  - copy buttons on the cheatsheet;
  - a search for "dotnetup";
  - a read of `samples/README.md` prerequisites.

  Record her answer in a devlog note.

## Workflow follow-up

- Whole-change review, the change-done note, and the offer of `code-review`, per CLAUDE.md
  "Finishing a change".
- Merge to `main` and push, which deploys the site, then check the live pages.
- Offer `/opsx:archive`.
