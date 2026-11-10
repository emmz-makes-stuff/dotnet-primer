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

- [x] 6.1 Add a `:::note` at the top of `## Windows (WSL 2)` on `/platform/docker/`. It says the
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

## 7. Review fixes: Rancher Desktop as the default

Agreed with Emmz on 2026-11-10. Rancher Desktop (free, Apache 2.0) becomes the default on Windows
and macOS: it is "more comfortable for developers who've never done anything there wasn't a menu
item for in Visual Studio". On Windows, Docker Engine in WSL 2 stays as an advanced option. On macOS,
OrbStack, Docker Desktop and Colima become the alternatives. Nothing is installed on this Mac: the
Rancher Desktop steps come from its docs, and the "not yet tested" note covers them on both
platforms.

- [x] 7.1 Record the Rancher Desktop facts (installation, requirements, container engine,
  Kubernetes, Testcontainers, Administrative Access, licence, latest release) in a devlog note with
  their URLs. Update the planning artifacts:
  - **The delta spec:** the Windows requirement covers Rancher Desktop as the default, with Docker
    Engine in WSL 2 as the advanced option (still loopback-only, and still saying it has no
    authentication). The macOS default is Rancher Desktop. The untested-note scenario covers
    Rancher Desktop on both platforms.
  - **The design:** D2 and D3 revised, plus the Risks.
  - **The proposal:** What Changes.

  Verify: `openspec validate getting-started-docker --strict`.
- [x] 7.2 Rewrite the Windows and macOS parts of `/platform/docker/`:
  - **Windows:** install WSL (`wsl --install`, as administrator); install Rancher Desktop from the
    MSI on its GitHub releases page, for everyone; on first run, choose **dockerd (moby)** and turn
    off **Enable Kubernetes**; set `DOCKER_HOST=npipe:////./pipe/docker_engine` for Windows tools
    (Testcontainers), as Rancher Desktop's Testcontainers guide says. Docker Desktop stays as an
    alternative. The current six steps, their caution and the dashboard paragraph move into a
    collapsed `<details>` block, "Advanced: Docker Engine in WSL 2, without a desktop app".
  - **macOS:** Rancher Desktop from its DMG (Apple silicon or Intel), the same first-run choices,
    and the Testcontainers pointer: Administrative Access for `/var/run/docker.sock`, or Rancher's
    three variables. OrbStack (and its licence), Docker Desktop and Colima become the alternatives.
  - **The notes:** the Windows note covers Rancher Desktop and the advanced WSL steps, and a new macOS
    note covers Rancher Desktop there. Each has its own prefilled issue ("Docker on Windows: " or
    "Docker on macOS: ") with prompts for the OS and Rancher Desktop versions.
  - **Check Docker:** works in PowerShell or the macOS terminal for Rancher Desktop. The
    `Invoke-RestMethod` check applies to the advanced path only.
  - **Pitfalls:** add one for "the `docker` command isn't found, or `nerdctl` is the only CLI"
    (containerd was chosen), whose fix is dockerd (moby) under Preferences, Container Engine. The
    existing ones are scoped to Linux and the advanced path.
  - **The frontmatter description.**

  Verify: `make gates`. The built page has its anchors. The `<details>` block renders with its code
  blocks (checked in `site/dist`), and the issue link decodes as intended.
- [x] 7.3 Update the other mentions to the new defaults: the samples README's Docker prerequisite,
  and the docker row of `docs/dependencies.md`. Verify: `make gates` and `make deps`.

## 8. Review fixes: code-review findings

Agreed with Emmz on 2026-11-10: fix all seven findings from the `code-review` run on
`change/getting-started-docker`.

- [x] 8.1 Docker page wording, in `site/src/content/docs/platform/docker.md`:
  - **The daemon pitfall:** drop "systemd is usually off" on the advanced path, which step 2
    contradicts. Name the likely causes instead: WSL stopped the idle distro, or the port override
    failed (which the last pitfall covers).
  - **The daemon pitfall:** also quote Rancher Desktop's own errors. That is
    `unix:///Users/<you>/.rd/docker.sock` on macOS, and the `npipe:////./pipe/docker_engine` pipe on
    Windows.
  - **`DOCKER_HOST`:** say to clear it when switching tools on Windows (Docker Desktop needs none,
    and the advanced path sets its own).
  - **Step 5:** point at the TLS pitfall from the step itself.

  Verify: `make gates`.
- [x] 8.2 Use the full `https://dotnet-primer.emmz.dev/platform/docker/` URL in the `make deps`
  hint and in the docker row of `docs/dependencies.md`. Verify: `make deps`.
- [x] 8.3 Move the .NET page's `## Get Docker` hand-off to the end of the page, after
  `## Check your setup`. This replaces task 2.1's placement. Update `design.md` where it records the
  placement. Verify: `make gates`.
- [x] 8.4 Add `dist.test.ts` checks for the spec:
  - Docker comes straight after the .NET 10 page in the sidebar's Getting started group;
  - the .NET 10 page links to `/platform/docker/`;
  - each of the Docker page's untested-platform notes links to a new issue whose query has
    `labels=content` and a title.

  Verify: `make gates`. Each check fails when its condition is broken by hand, then passes again.

## 9. Review fixes: spec for the Get Docker move

Found by the archive's sync check on 2026-11-10. The main spec says the .NET 10 page SHALL end with
the setup check, but 8.3 put "Get Docker" after it. Emmz chose to amend the spec and keep the page.

- [x] 9.1 Add a MODIFIED "Getting started page ends with a setup check" to the delta spec
  `topic-page-content`: the recipe stays last, and only a short pointer to the Docker page may
  follow it. Verify: `openspec validate getting-started-docker --strict` and `make gates`.

## Workflow follow-up

- Whole-change review, the change-done note, and the offer of `code-review`, per CLAUDE.md
  "Finishing a change".
- Merge to `main` and push, which deploys the site, then check the live page.
- Offer `/opsx:archive`.
