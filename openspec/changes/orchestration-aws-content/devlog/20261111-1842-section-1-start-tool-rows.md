# Section 1 started: the Aspire CLI and AWS CLI rows (1.1)

<!-- Section: 1 · Task: 1.1 · Base: 384453b -->

Emmz agreed both rows on 2026-11-11, while we planned the change.

- **`aspire`:** the Aspire CLI, so that the page and the probe use aspire.dev's own path
  (`aspire new`, `aspire run`) rather than the templates or a hand-written AppHost. The check is
  `aspire --version | grep -q '^13\.'`. aspire.dev's *Install Aspire CLI* page documents
  `aspire --version` printing `13.6.0+{commitSHA}`, and this Mac has `13.6.0+56f3e9c0…`. For the
  install, the row names aspire.dev's script (`curl -sSL https://aspire.dev/install.sh | bash`,
  `irm https://aspire.dev/install.ps1 | iex` on Windows), which is how this Mac got it
  (`~/.aspire/bin/aspire`), and Homebrew's `brew install --cask microsoft/aspire/aspire`. The page
  also lists npm, NuGet, WinGet, mise and Nix. No gate runs it.
- **`aws`:** AWS CLI v2, only to syntax-check the page's AWS commands (`help`, and
  `--generate-cli-skeleton` for the ECS task definition's keys). **No auth check:** nothing runs
  against AWS (D8), so a sign-in would be a requirement the change doesn't have. The check is
  `aws --version | grep -q '^aws-cli/2\.'`. This Mac has `aws-cli/2.36.34`, from AWS's installer
  (`~/.local/bin/aws`).

Verified: `make deps` gives `DEPS_EXIT:0` (unsandboxed). With `PATH=/usr/bin:/bin`, it prints both
`MISSING aspire CLI 13.x …` and `MISSING aws CLI v2 …`, so the checks do fail when a tool is absent.

## Next

1.2: probe health checks and shutdown (D2–D6).
