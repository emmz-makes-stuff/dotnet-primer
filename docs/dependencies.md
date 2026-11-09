# Dependencies

<!-- Maintained by /emmz:architecture; verified by `make deps`. -->

Checks run from the repo root. The dotnet check looks for an installed 10.0.4xx SDK directly. The root
`global.json` pins builds to that band, which matters on machines whose default SDK is newer, e.g. an
11 preview.

| Tool | Why | Version | Check | Auth check | Install / auth |
|---|---|---|---|---|---|
| node | site build/test/format/lint (Astro 7 needs ≥22.23) | >=22.23 | `node -e "const[a,b]=process.versions.node.split('.').map(Number);process.exit(a>22\|\|(a===22&&b>=23)?0:1)"` | — | `brew install node` |
| npm | site package manager | >=9.6.5 | `npm --version` | — | ships with node |
| dotnetup | install/manage SDKs; verify the Getting started page | any 0.x preview | `dotnetup --version` | — | `curl -fsSL https://aka.ms/dotnetup/get-dotnetup.sh \| bash` (Learn: *Get started with dotnetup*) |
| dotnet | samples build/test/format | SDK 10.0.4xx (pinned by `global.json`) | `dotnet --list-sdks \| grep -q '^10\.0\.4'` | — | `dotnetup sdk install 10.0.4xx` (from the repo root) |
| docker | Testcontainers in sample tests; container exercises | any recent, daemon running | `docker info --format '{{.ServerVersion}}'` | — | install Docker Desktop or OrbStack (macOS) |
| openspec | change tracking | >=1.15 | `openspec --version` | — | `npm i -g @fission-ai/openspec` |
| gh | PRs, Pages settings, issues | any | `gh --version` | `gh auth status` | `brew install gh`, then `gh auth login` |
| git | source control (SSH remote `emmz-makes-stuff/dotnet-primer`) | any | `git --version` | — | `xcode-select --install` |

Not machine dependencies, but needed once for deployment:

- **GitHub Pages**: repo Settings → Pages → Source: *GitHub Actions*; custom domain
  `dotnet-primer.emmz.dev`; tick *Enforce HTTPS* once the certificate is issued.
- **Cloudflare DNS** (zone `emmz.dev`): `CNAME dotnet-primer → emmz-makes-stuff.github.io`, set to
  DNS-only until GitHub issues the certificate.
