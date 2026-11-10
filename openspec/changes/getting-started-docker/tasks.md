# Tasks

## 1. Facts check and Linux probe

- [x] 1.1 Check the D7 sources and write a devlog note. Record exactly what each says on the day:
  - Docker's "Install Docker Engine on Ubuntu" page: the apt commands, verbatim;
  - Microsoft's WSL install page and its systemd page (`wsl --install -d Ubuntu`, whether systemd
    is on by default);
  - Docker's Desktop licence FAQ (the thresholds);
  - OrbStack's licensing page;
  - Colima's README (the Homebrew install and `colima start`);
  - Docker's Engine docs on TCP without TLS: the flag or warning the current dockerd needs.

  Verify: the note quotes each source with its URL. Every URL returns 200 with no redirect (checked
  through `ctx_execute` `fetch`).
- [x] 1.2 Run the Linux probe (design D8) in a throwaway OrbStack Ubuntu machine (unsandboxed):
  - run D2 steps 3–5 and D4 verbatim, as the page will show them: the apt repository, the packages,
    the `docker` group, the systemd drop-in with the TCP endpoint, then reload and restart;
  - run D5's checks (`hello-world`, `docker info --format '{{.ServerVersion}}'`);
  - from inside the machine, run `curl -s http://127.0.0.1:2375/version`;
  - run `sudo systemctl restart docker`, and check the daemon comes back with both endpoints;
  - delete the machine.

  Verify: a devlog note with the exact commands, their output, the Engine version, and the TLS flag
  the drop-in needed (if any). Any command that differs from design D2/D4 is corrected in the design
  in the same section.

## 2. The Get Docker section

- [x] 2.1 Add `## Get Docker` to `site/src/content/docs/platform/dotnet-10-and-cli.md`, after the
  dotnetup sections and before "Pin the SDK with global.json" (design D1):
  - an opening that says what the primer uses Docker for, links the pages that need it, and says
    that exercises 01–06 don't;
  - `### Windows (WSL 2)` with D2's steps 1–6 as numbered, copyable steps, using the commands
    proven in 1.2. Include the security `:::caution`, the dashboard-on-Windows paragraph, the Docker
    Desktop alternative with its licence terms, and the optional `docker.exe` line;
  - `### macOS`: OrbStack as the default, then Docker Desktop and Colima with their licence terms
    and the Colima socket pointer (D3);
  - `### Linux`: Docker's repository, the `docker` group, and not the distro's `docker.io` (D4);
  - `### Check Docker`: D5's checks and their expected output, including the Windows PowerShell
    check.

  Verify: each command matches 1.1/1.2 exactly. Licence terms say "at the time of writing" and link
  each vendor's terms. `make site-build`, `make site-lint` and `make site-format` pass.
- [x] 2.2 Add D6's four pitfalls to the page's Pitfalls list, in its existing "problem. Fix:" style,
  and add Docker's Engine install page and the WSL install page to the frontmatter `learn:` links.
  Verify: each pitfall's error text matches what 1.2 printed, or what the docs quote. `make gates`
  passes.

## 3. Point the other Docker mentions at the section

- [x] 3.1 Link the new section from the places that mention Docker:
  - the Docker prerequisite in `samples/README.md` (it names the WSL 2 path and links to
    `/platform/dotnet-10-and-cli/#get-docker`);
  - the dashboard paragraph on `observability/logging.md` ("Start it with Docker", linked);
  - the install hint in the docker row of `docs/dependencies.md`.

  Verify: `make site-build`, then check the anchor exists in `site/dist` and that each link targets
  it. `make gates` passes.

## 4. Human check

- [x] 4.1 Human check (stop and ask). Two parts:
  - **Windows** (Emmz, or a Windows 11 tester she names): follow the Windows recipe from a clean
    WSL install. Report:
    - the output of the D5 checks, in WSL and in PowerShell;
    - that the dashboard `docker run` in WSL opens at `http://localhost:18888` in the Windows
      browser;
    - that `DOCKER_HOST` is set in a new Windows terminal;
    - that the daemon is back after `wsl --shutdown`.
  - **Site** (Emmz): preview the page in light and dark. Check the new section's copy buttons, the
    caution, and search for "WSL" and "Docker".

  Record the answers in a devlog note. If the Windows forwarding fails, stop and agree the D2
  fallback (Risks) before changing the page.

  **Revised by §5:** run this against the new `/platform/docker/` page, after §5 has landed.

  **Revised on 2026-11-10 (Emmz):** the site part passed. Nobody is available to run the Windows
  recipe, so the Windows part is **deferred**, not done. Emmz accepted shipping the Windows steps
  untested on Windows, with §6's prefilled issue link as the way readers report a failure.

## 5. Review fixes: Docker as its own page

Agreed with Emmz on 2026-11-10, while she reviewed the §2 copy. Docker gets its own page,
`/platform/docker/`, titled "Docker". It is listed in Getting started, straight after ".NET 10 and the
dotnet CLI", and that page keeps a one-line pointer to it.

- [x] 5.1 Update the planning artifacts to match. In the delta spec (`topic-page-content`), the three
  requirements are about "the Docker page", not a Getting started section, and the page follows the
  topic-page structure (an outcome, the concepts, then Pitfalls). Add a `site-structure` delta: the
  Getting started group lists the Docker page after ".NET 10 and the dotnet CLI". Revise design D1,
  noting that this replaces the rejected alternative, and the proposal's What Changes. Verify:
  `openspec validate getting-started-docker --strict` passes.
- [x] 5.2 Move the content to `site/src/content/docs/platform/docker.md`:
  - frontmatter with the title "Docker", a description, and `learn:` with "Install WSL";
  - an opening outcome sentence, then the existing opening text;
  - `## Windows (WSL 2)`, `## macOS`, `## Linux` and `## Check Docker`, promoted from `###`, with
    the text unchanged;
  - `## Pitfalls` with the four Docker pitfalls, moved from the .NET 10 page.

  Add `{ slug: "platform/docker" }` to the Getting started sidebar after `platform/dotnet-10-and-cli`,
  and add the slug to `EXPECTED_SLUGS` and `FULL_CONTENT_SLUGS` in `site/tests/dist.test.ts`. On the
  .NET 10 page, replace the section with a one-line pointer to `/platform/docker/` (01–06 don't need
  it), and remove the four pitfalls and the "Install WSL" Learn link. Fix the in-page anchors
  (`#windows-wsl-2`, `#check-docker`) so they resolve on the new page. Verify: `make gates`. The
  built page has the five headings' anchors, and `site/dist` has no link to
  `/platform/dotnet-10-and-cli/#get-docker` left anywhere.
- [x] 5.3 Repoint the §3 links (samples README, the Logging page's dashboard paragraph,
  `docs/dependencies.md`) to `/platform/docker/`. Verify: `make gates`, `make deps`, and a grep for
  `#get-docker` across the repo that finds only devlog notes.

## 6. Review fixes: report Windows problems

Agreed with Emmz on 2026-11-10. With no Windows tester, the Windows steps ship untested on Windows.
The page says so, and gives a prefilled issue link.

- [ ] 6.1 Add a `:::note` at the top of `## Windows (WSL 2)` on `/platform/docker/`. It says the
  steps have been checked on Linux but not yet end to end on Windows, and asks readers to open an
  issue if a step fails. The link opens
  `https://github.com/emmz-makes-stuff/dotnet-primer/issues/new` with:
  - the title "Docker on Windows: ";
  - the `content` label;
  - a body asking for the Windows version (`winver`), the WSL version (`wsl --version`), the step
    that failed, and the command and its output.

  Update the delta spec's Windows requirement with a scenario for it, and the design's Risks.
  Verify: the link's query string decodes to that title, label and body (checked with
  `new URL(…).searchParams`). `openspec validate --strict` and `make gates` pass.

## Workflow follow-up

- Whole-change review, the change-done note, and the offer of `code-review`, per CLAUDE.md
  "Finishing a change".
- Merge to `main` and push, which deploys the site, then check the live page.
- Offer `/opsx:archive`.
