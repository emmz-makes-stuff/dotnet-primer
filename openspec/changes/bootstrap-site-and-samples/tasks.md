# Tasks

## 1. Repo foundation

- [x] 1.1 Add root `.gitignore` (`node_modules/`, `site/dist/`, `site/.astro/`, `bin/`, `obj/`, `.DS_Store`, `*.binlog`) and `.editorconfig` (C# conventions used by `dotnet format`, 2-space for web files); verify `git status` shows neither `node_modules` nor build output after later groups build
- [x] 1.2 Add root `global.json` per design D9 (SDK 10.0.400, `rollForward: latestFeature`, test runner Microsoft.Testing.Platform); verify `dotnet --version` from the repo root prints a `10.0.4xx` version, not the 11 RC
- [x] 1.3 Add root `README.md`: what the primer is, the `site/` and `samples/` layout, the eight gate commands verbatim, and a link to `docs/`; verify every command in it runs once groups 2 and 7 land

## 2. Site scaffold

- [x] 2.1 Create `site/` as an Astro 7 + Starlight project (`package.json` with `engines.node >=22.23`, `.nvmrc` = `24`, `tsconfig.json`, `astro.config.mjs` with `site: 'https://dotnet-primer.emmz.dev'`, `editLink.baseUrl`, `customCss`, `lastUpdated`; design D2), commit `package-lock.json`; verify `npm ci --prefix site` succeeds
- [x] 2.2 Add `site/public/CNAME` containing exactly `dotnet-primer.emmz.dev`; verify it is copied to `site/dist/CNAME` by `npm --prefix site run build`
- [x] 2.3 Add npm scripts `build`, `preview`, `lint` (`astro check`), `format`, `format:check` (`prettier --check .`) with `@astrojs/check`, `typescript`, `prettier`, `prettier-plugin-astro`, `.prettierrc.json` (`proseWrap: preserve`) and `.prettierignore` (design D8); verify `npm --prefix site run build`, `npm --prefix site run lint` and `npm --prefix site run format:check` each exit 0 on the scaffold, and `format:check` exits non-zero on a deliberately mis-formatted file (then revert)

## 3. Content-page conventions

- [x] 3.1 Extend `docsSchema` in `site/src/content.config.ts` with required `learn: [{title, url}]` (min 1, URL must start `https://learn.microsoft.com/`; design D4); verify `npm --prefix site run build` fails naming the page when a scratch page omits `learn`, and when it uses a non-Learn URL (then remove the scratch page)
- [x] 3.2 Override `MarkdownContent` to render the page body followed by an "On Microsoft Learn" block from `entry.data.learn`; verify in the built HTML that a page's Learn links appear in that block
- [x] 3.3 Override `EditLink` to render the stock edit link plus "Report a problem" linking to `https://github.com/emmz-makes-stuff/dotnet-primer/issues/new` with prefilled `title` and `body` (page title and full `https://dotnet-primer.emmz.dev/` URL; design D5), built without client JS; verify the decoded `href` in built HTML for a nested page, and that it renders on the splash `index` page (fall back to a `Footer` override if not)
- [x] 3.4 Write `site/README.md` authoring guide: how to add a page, the required `learn` frontmatter, sidebar placement, code fences and languages, `prev`/`next` on standalone pages; verify by following it to add one of the group-4 pages

## 4. Pages and learning path

- [x] 4.1 Configure the explicit `sidebar` in `astro.config.mjs` with the groups and order from design D3; verify `npm --prefix site run build` succeeds and the built sidebar lists ASP.NET Core → Configuration → Dependency Injection → Middleware before the remaining path pages
- [x] 4.2 Write the introduction `index.mdx` (splash template): who the primer is for, how it's organised (learning path, standalone sections, cheatsheets, exercises), a card grid linking every area and standalone section, `learn` frontmatter; verify every D3 slug is linked from `dist/index.html`
- [x] 4.3 Add a stub page for each subject area in D3 (title, description, a short "what this covers" paragraph, ≥1 `learn` link, a "Coming soon" note), with standalone `csharp/` and `coming-from-3-5/` pages setting `prev: false`/`next: false`; verify each builds and appears under its sidebar group
- [x] 4.4 Add the `cheatsheets/dotnet-cli` stub (a handful of real `dotnet new`/`build`/`run`/`test`/`add package` commands in `sh` code blocks, `learn` link to the dotnet CLI reference), linked from `platform/dotnet-10-and-cli`; verify both pages build and the link resolves
- [x] 4.5 On `aspnet-core/configuration`, describe exercise 01 (goal, steps, how to run tests against the starter) linking to `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/01-configuration`, with a `csharp` sample; verify the link and the code block render in built HTML

## 5. Site test gate

- [x] 5.1 Add `vitest`, `node-html-parser`, `vitest.config.ts`, and `npm test` = `vitest run`; `tests/dist.test.ts` fails with "run `npm --prefix site run build` first" when `site/dist/` is absent; verify by running `npm --prefix site test` on a clean tree
- [x] 5.2 Implement the content-page assertions (Learn link in block; "Report a problem" href and decoded body URL) per design D7; verify they pass on the built site and fail when a page's Report link is removed from the override (then revert)
- [x] 5.3 Implement the internal-link resolution check reporting `source → target`; verify it passes, then fails on a scratch page linking `/no-such-page/` (then remove it)
- [x] 5.4 Implement the structure checks (index links every D3 slug; `dist/pagefind/pagefind.js` exists; DI page has `data-pagefind-body`) and the `dist/CNAME` check; verify `npm --prefix site test` exits 0

## 6. Theme starting point

- [ ] 6.1 Write `site/src/styles/theme.css` overriding Starlight accent/gray/font/width tokens for light and dark, with self-hosted `@fontsource-variable/*` fonts (design D6); record the chosen typefaces and palette in a devlog note; verify build, lint and format gates still pass and both colour schemes render legibly in `npm --prefix site run preview`

## 7. Samples

- [ ] 7.1 Add `samples/Directory.Build.props` and `samples/Directory.Packages.props` per design D9 (net10.0, nullable, implicit usings, warnings-as-errors, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild`; central versions for `xunit.v3` and `Microsoft.AspNetCore.Mvc.Testing` 10.0.x resolved at apply time); verify `dotnet build` of an empty solution succeeds
- [ ] 7.2 Create exercise `01-configuration`: `Solution` minimal API binding `GreetingOptions` from `Greeting` and serving `GET /greeting`; `Starter` identical shape with `TODO`s returning 501; both `public partial class Program`; verify both compile with zero warnings
- [ ] 7.3 Add shared tests in `exercises/01-configuration/tests/` (configured message returned; in-memory/env override wins) with descriptive failure messages, compiled into `Starter.Tests` and `Solution.Tests` via `<Compile Include="../tests/**/*.cs" />`; verify `dotnet test --project …/Solution.Tests` passes and `dotnet test --project …/Starter.Tests` fails with the descriptive messages
- [ ] 7.4 Create `samples/Primer.slnx` (all four projects) and `samples/Primer.Solutions.slnx` (Solution + Solution.Tests); verify `dotnet build samples/Primer.slnx`, `dotnet test --solution samples/Primer.Solutions.slnx` and `dotnet format samples/Primer.slnx --verify-no-changes` each exit 0
- [ ] 7.5 Write `samples/README.md`: prerequisites (SDK 10.0.4xx, Docker for later exercises), layout, how to work an exercise red→green against the starter, practice-only note; verify its commands run as written

## 8. GitHub Actions

- [ ] 8.1 Add `.github/workflows/site.yml` per design D10 (gates job running the four site gate commands verbatim then `upload-pages-artifact`; `deploy` job on `main` non-PR only via `deploy-pages`; permissions and concurrency as specified; action major versions resolved at apply time); verify with `actionlint` if available, else by YAML parse and review against D10
- [ ] 8.2 Add `.github/workflows/samples.yml` (path-filtered; `setup-dotnet` from `global.json`; the three samples gates); verify the same way as 8.1

## 9. Integration check

- [ ] 9.1 Run all eight gate commands from a clean tree (`site/dist` removed, then build first) and record each exit code in a devlog note; verify all exit 0
- [ ] 9.2 In `npm --prefix site run preview`, search "Dependency Injection" and confirm the DI page is a result; copy a code block from the cheatsheet and confirm only the code is copied; record both in the devlog

## Workflow follow-up

- Emmz makes `emmz-makes-stuff/dotnet-primer` **public** (it is currently private): needed for free GitHub Pages, the "Report a problem" issue links and the exercise links (found in §4).
- Emmz pushes `main` to `emmz-makes-stuff/dotnet-primer` (outward-facing — confirm before pushing).
- Emmz sets GitHub Pages source to *GitHub Actions* and custom domain `dotnet-primer.emmz.dev`; adds Cloudflare `CNAME dotnet-primer → emmz-makes-stuff.github.io` DNS-only; ticks *Enforce HTTPS* once the certificate is issued.
- Confirm the first `site.yml` run deploys and `https://dotnet-primer.emmz.dev/` serves the introduction over HTTPS.
- Archive the change after review.
