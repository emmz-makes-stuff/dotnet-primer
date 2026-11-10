# Stopped: human check (task 4.1), on the Docker page

<!-- Section: 4 · Tasks: 4.1 blocked on Emmz · Base: 82eb07c -->

§1–§3 and §5 are committed (`7d5f622`, `344c102`, `cd7af14`, `82eb07c`), with `GATES_EXIT:0`.
This note replaces `20261110-0015-stopped-human-check.md`, whose URLs are out of date now that §5
has moved the content to `/platform/docker/`.

(Housekeeping: the §3 note is named `0014`, but its commit is at 00:13. The order is still right,
so it was left alone rather than rewriting history.)

## Questions for Emmz

1. **The site.** Does the Docker page look right in light and dark? Is it in the right place in
   Getting started, and is the .NET 10 page's pointer right?
2. **Windows.** Who runs the Windows recipe, and on which Windows 11 machine? The thing still
   unproven is that Windows' `localhost:2375` reaches dockerd in WSL bound to `127.0.0.1` only.

## Recipe

**A. The site** (from the repo root):

```sh
npm --prefix site run build && npm --prefix site run preview
```

In light and dark, check:
- **The sidebar:** Getting started lists ".NET 10 and the dotnet CLI", then "Docker".
- **The .NET 10 page:** "Get Docker" is one sentence linking the Docker page, and its Pitfalls no
  longer has the Docker items.
- **`/platform/docker/`:**
  - the outcome sentence;
  - the numbered Windows steps, with their code blocks indented under each step;
  - the caution box;
  - the copy buttons on the apt block and the `override.conf` block;
  - macOS, Linux and Check Docker;
  - the four pitfalls.
- **The introduction page:** it has a Docker card.
- **Search:** "WSL" and "Docker" should find the Docker page.

**B. Windows** (Windows 11, preferably with no WSL or Docker yet): follow
`/platform/docker/#windows-wsl-2`, steps 1–6, then report:

1. the output of the two `docker` commands in "Check Docker", in the Ubuntu terminal;
2. `(Invoke-RestMethod http://localhost:2375/version).Version` in a new PowerShell;
3. `echo $env:DOCKER_HOST` in a new PowerShell, which should print `tcp://localhost:2375`;
4. after `wsl --shutdown` and reopening Ubuntu, that `docker info --format '{{.ServerVersion}}'`
   works with no manual start;
5. optionally, the Logging page's dashboard `docker run` in Ubuntu, opened at
   `http://localhost:18888` in the Windows browser.

## On PASS

Tick 4.1. Write the section-done note with the answers (the machine, the Windows build, the
outputs), and commit as `feat(getting-started-docker): human check (§4)`. Then "Finishing a change":
the whole-change review from `df92b93`, the change-done note, and the offer of `code-review`.

## On FAIL

- **If 2 fails:** stop and agree the design's fallback first (bind the WSL VM's address in
  `DOCKER_HOST`, or use mirrored networking).
- **Anything else:** record exactly what failed, add `## 6. Review fixes: <theme>` to `tasks.md`,
  agree it with her, and build it with the section loop.
