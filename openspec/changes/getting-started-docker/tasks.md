# Tasks

## 1. Facts check and Linux probe

- [ ] 1.1 Check the D7 sources and write a devlog note. Record exactly what each says on the day:
  - Docker's "Install Docker Engine on Ubuntu" page: the apt commands, verbatim;
  - Microsoft's WSL install page and its systemd page (`wsl --install -d Ubuntu`, whether systemd
    is on by default);
  - Docker's Desktop licence FAQ (the thresholds);
  - OrbStack's licensing page;
  - Colima's README (the Homebrew install and `colima start`);
  - Docker's Engine docs on TCP without TLS: the flag or warning the current dockerd needs.

  Verify: the note quotes each source with its URL. Every URL returns 200 with no redirect (checked
  through `ctx_execute` `fetch`).
- [ ] 1.2 Run the Linux probe (design D8) in a throwaway OrbStack Ubuntu machine (unsandboxed):
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

- [ ] 2.1 Add `## Get Docker` to `site/src/content/docs/platform/dotnet-10-and-cli.md`, after the
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
- [ ] 2.2 Add D6's four pitfalls to the page's Pitfalls list, in its existing "problem. Fix:" style,
  and add Docker's Engine install page and the WSL install page to the frontmatter `learn:` links.
  Verify: each pitfall's error text matches what 1.2 printed, or what the docs quote. `make gates`
  passes.

## 3. Point the other Docker mentions at the section

- [ ] 3.1 Link the new section from the places that mention Docker:
  - the Docker prerequisite in `samples/README.md` (it names the WSL 2 path and links to
    `/platform/dotnet-10-and-cli/#get-docker`);
  - the dashboard paragraph on `observability/logging.md` ("Start it with Docker", linked);
  - the install hint in the docker row of `docs/dependencies.md`.

  Verify: `make site-build`, then check the anchor exists in `site/dist` and that each link targets
  it. `make gates` passes.

## 4. Human check

- [ ] 4.1 Human check (stop and ask). Two parts:
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

## Workflow follow-up

- Whole-change review, the change-done note, and the offer of `code-review`, per CLAUDE.md
  "Finishing a change".
- Merge to `main` and push, which deploys the site, then check the live page.
- Offer `/opsx:archive`.
