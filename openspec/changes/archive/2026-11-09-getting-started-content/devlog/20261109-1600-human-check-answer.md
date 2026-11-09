# Human check answer (task 4.1)

<!-- Section: 4 · Tasks: 4.1 · Base: 80afc75 -->

Emmz previewed the site.

- **Passed:** both pages in light and dark (including the caution aside), the cheatsheet copy buttons,
  the "dotnetup" search, and the `samples/README.md` prerequisites.
- **Finding:** `/cheatsheets/dotnet-cli/` doesn't say how to install `dotnetup` itself. It should do
  so for Windows, macOS and Linux.

**Agreed fix:** §5, "Review fixes: dotnetup install on the cheatsheet".

- Add a "Get dotnetup" block at the top of "Install and select SDKs". There are two copyable blocks,
  as on Learn:
  - `sh` for macOS and Linux, which share the script;
  - `powershell` for Windows.
- Then come `dotnetup init` and `dotnetup --version`, for a new terminal.
- Emmz picked this shape over three separate OS blocks, and over Starlight tabs, which would need
  `.mdx`.

4.1 is ticked: the check is done, and its one finding moves to §5.

## Next

- §5, task 5.1: add the Get dotnetup blocks to the cheatsheet.
- Then 5.2: Emmz re-checks the cheatsheet in the preview.
