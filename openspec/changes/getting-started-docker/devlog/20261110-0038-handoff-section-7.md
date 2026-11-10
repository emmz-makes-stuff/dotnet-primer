# Handoff: §7 is mid-section (7.1 and 7.2 done, uncommitted; 7.3 left)

<!-- Section: 7 · Tasks: 7.1–7.3 · Base: d46ccf7 -->

Context passed 40% after 7.2, so this hands off before 7.3. §7 is **uncommitted on purpose**: the
section loop commits it once, when 7.3 is done.

## State of §7

- **7.1 (done, ticked):** the facts are in `20261110-0035-rancher-desktop-facts.md`. The spec, design
  (D2 relabelled as advanced, D3 revised, D9 new, the "untested" risk widened) and proposal are
  updated. `openspec validate --strict` passes.
- **7.2 (done, ticked):** `site/src/content/docs/platform/docker.md` was rewritten by
  `<scratchpad>/rancher-page.mjs` (scratch, not in the repo).
  - **The page:**
    - `## Windows` (the anchor is now `#windows`, not `#windows-wsl-2`) has a Windows note, four
      Rancher Desktop steps (`wsl --install --no-distribution`, which is confirmed in Learn's WSL
      basic-commands page; the MSI "for everyone"; Preferences: dockerd (moby) and Kubernetes
      unticked; `DOCKER_HOST=npipe:////./pipe/docker_engine`), the Docker Desktop alternative, and
      `<details id="docker-engine-in-wsl-2">`, "Advanced: Docker Engine in WSL 2, without a desktop
      app". That block holds the old steps 1–6 verbatim, the caution (now "use Rancher Desktop
      instead (above)"), the dashboard paragraph, and the `Invoke-RestMethod` check;
    - `## macOS` has a macOS note, the DMG steps, the Administrative Access / Testcontainers-guide
      pointer, and the OrbStack, Docker Desktop and Colima alternatives;
    - `## Linux` links `#docker-engine-in-wsl-2` for the Ubuntu commands;
    - `## Check Docker` is for PowerShell, macOS or Linux;
    - `## Pitfalls` has a new first item (containerd chosen, so `docker` isn't found), and the old
      four are scoped to Linux and the advanced path.
  - **Decision: one note per platform**, each with its own issue title, "Docker on Windows: " or
    "Docker on macOS: ". The body prompts for the OS version and the Rancher Desktop version (or the
    WSL version on the advanced path). This matches the spec requirement "near the top of that
    platform's part".
  - **Rancher Desktop's first-run dialog isn't documented** on the pages fetched, so step 3 uses the
    documented Preferences pages, and says to make the same choices if a first-run window asks.
  - **Checked:**
    - `GATES_EXIT:0`;
    - in `site/dist`, the ids `windows`, `macos`, `linux`, `check-docker`, `pitfalls` and
      `docker-engine-in-wsl-2`;
    - the details block has 8 code blocks, the numbered list, and the caution aside;
    - both issue links decode correctly;
    - no `#windows-wsl-2` links remain.
  - **Gotcha:** Starlight's mobile table of contents is also a `<details>`. Find the advanced block
    by `id="docker-engine-in-wsl-2"`, not by the first `<details`.
- **Uncommitted files:**
  - `openspec/changes/getting-started-docker/{design.md,proposal.md,tasks.md,specs/topic-page-content/spec.md}`;
  - `devlog/20261110-0035-rancher-desktop-facts.md`;
  - this note;
  - `site/src/content/docs/platform/docker.md`.

## Other

- `change/getting-started-docker` has never been pushed (no upstream). Nothing is running in the
  background.
- The interrupted whole-change review (started after §6) has to run again after §7, from `df92b93`.

## Next

- **Task 7.3:**
  - change the samples README's Docker prerequisite (`samples/README.md` line 17, "Docker Engine in
    WSL 2 on Windows, OrbStack on macOS, or Docker Engine on Linux") to Rancher Desktop on Windows
    and macOS, and Docker Engine on Linux;
  - make the same change to the docker row of `docs/dependencies.md`;
  - check that the intro card's description ("A container engine on Windows, macOS or Linux") still
    fits;
  - verify with `make gates` and `make deps`.

  Then tick 7.3, write the §7 section-done note (with the gate lines), and make **one commit** for
  §7: `feat(getting-started-docker): Rancher Desktop as the default (§7)`, with 7.1–7.3 in the body.
- **Then "Finishing a change":** the whole-change review from `df92b93`, the change-done note, and
  the offer of `code-review`.
- **Emmz's standing decisions:**
  - Rancher Desktop is the default on Windows and macOS;
  - the Engine-in-WSL steps are kept as the advanced option;
  - Rancher Desktop is not installed on this Mac;
  - the Windows and macOS steps are untested on purpose, with an issue link in each note.
