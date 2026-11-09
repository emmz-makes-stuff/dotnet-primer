# Stopped: Emmz re-checks the cheatsheet (task 5.2)

<!-- Section: 5 · Tasks: 5.1–5.2 · Base: bd36cc1 -->

**5.1 is done (uncommitted).** The cheatsheet's "Install and select SDKs" section now opens with
"Get `dotnetup`":

- an `sh` block for macOS and Linux (`curl -fsSL https://aka.ms/dotnetup/get-dotnetup.sh | bash`);
- a `powershell` block for Windows (`irm https://aka.ms/dotnetup/get-dotnetup.ps1 | iex`);
- then, in a new terminal, `dotnetup init` and `dotnetup --version`.

The commands are copied verbatim from Learn's "Get started with dotnetup". Both `aka.ms` URLs return
200, served from `ci.dot.net/public/dotnetup/0.3.0-preview.1.26554.3/…`. The scripts weren't run, per
D2: they write `~/.dotnetup`, and Emmz already has `dotnetup`.

`make gates` gave `GATES_EXIT:0`, and the built page includes the `powershell` block.

**Waiting on Emmz** to preview `/cheatsheets/dotnet-cli/` in light and dark, with the copy buttons on
the new blocks.

## Next

- On her yes: tick 5.2, write the section-5-done note, and commit §5 ("Review fixes: dotnetup install
  on the cheatsheet").
- Then the whole-change review (`git diff 5a2682c..HEAD`), the change-done note, the offer of
  `code-review`, merge and push, check the live pages, and offer `/opsx:archive`.
