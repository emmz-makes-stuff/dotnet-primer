# Stopped: §4 human check (task 4.1)

<!-- Section: 4 · Tasks: 4.1 blocked on Emmz · Base: cd7af14 -->

§1–§3 are committed (`7d5f622`, `344c102`, `cd7af14`), with `GATES_EXIT:0`. 4.1 needs a person:
this Mac can't run WSL, and the look of the page needs checking in a browser.

## Questions for Emmz

1. **Site check.** Does the Get Docker section look right in light and dark? Check the copy buttons,
   the caution, and the search results for "WSL" and "Docker".
2. **Windows check.** Who runs the Windows recipe, and on which Windows 11 machine? The one thing
   §1 couldn't prove is that Windows' `localhost:2375` reaches a port that dockerd in WSL binds to
   `127.0.0.1` only. The WSL docs say localhost works, but not for loopback-only binds.

## Recipe

**A. The site** (from the repo root):

```sh
npm --prefix site run build && npm --prefix site run preview
```

Open `/platform/dotnet-10-and-cli/#get-docker` in light and dark. Check:
- the five subsections;
- the numbered Windows steps, and that the code blocks inside them are indented under each step;
- the caution box;
- the copy button on the long apt block and on the `override.conf` block;
- the four new pitfalls at the end of Pitfalls.

Then search for **WSL** and **Docker**: both should find this page. Also check that the Logging
page's "Start it with Docker" paragraph links here.

**B. Windows** (Windows 11, preferably one that has never had WSL or Docker): follow
`/platform/dotnet-10-and-cli/#windows-wsl-2` exactly, steps 1–6, then report:

1. the output of the two `docker` commands in "Check Docker", in the Ubuntu terminal;
2. the output of `(Invoke-RestMethod http://localhost:2375/version).Version` in a new PowerShell;
3. that `echo $env:DOCKER_HOST` in a new PowerShell prints `tcp://localhost:2375`;
4. that after `wsl --shutdown` and reopening Ubuntu, `docker info --format '{{.ServerVersion}}'`
   works without starting anything by hand;
5. optionally, that the dashboard works. Run the Logging page's `docker run` in Ubuntu, then open
   `http://localhost:18888` in the Windows browser.

## On PASS

Tick 4.1. Write the section-done note with the answers (which machine, the Windows build, the
outputs), and commit §4 as `feat(getting-started-docker): human check (§4)`. Then "Finishing a
change": the whole-change review from `df92b93` (the plan commit), the change-done note, and the
offer of `code-review`.

## On FAIL

- **If 2 fails** (Windows can't reach `localhost:2375`): stop and agree the design's fallback with
  Emmz before changing the page. The fallback is to bind the WSL VM's address and put it in
  `DOCKER_HOST`, or to use mirrored networking.
- **Anything else:** record exactly what failed (step, output, theme, element), add a
  `## 5. Review fixes: <theme>` section to `tasks.md`, agree it with her, and build it with the
  section loop.
