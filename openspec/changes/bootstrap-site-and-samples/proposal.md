# Proposal

## Why

Engineers who have spent 15 years on a .NET 3.5 codebase need to get up to speed on .NET 10, ASP.NET
Core, EF Core, gRPC and containers, and a migration is under way (`docs/DISCOVERY.md`). The repo is
empty. Before any real content can be written, it needs a working site skeleton, a working
companion-samples skeleton, and a gated deploy. With those in place, every later content change only
adds pages and exercises.

## What Changes

- Add a static documentation site under `site/`, following ADR-0001:
  - an **introduction** page that links down into every subject area;
  - one **stub page per subject area**, arranged as the learning path with ASP.NET Core as the spine,
    leading with Configuration → Dependency Injection → Middleware;
  - the **C# language** and **coming from .NET 3.5** sections, standalone from the path;
  - one **cheatsheet** stub (`dotnet` CLI commands) to prove the cheatsheet layout;
  - site-wide **search**, and code blocks with a **copy button**;
  - on every page, a **"Report a problem" GitHub issue link** prefilled with the page, and at least one
    **Microsoft Learn link**;
  - a **custom theme** as the starting point for the distinctive visual design.
- Add companion samples under `samples/`, following ADR-0003:
  - a .NET 10 SDK pin (root `global.json`) and shared build strictness;
  - two solution files: all projects, and solutions only;
  - **one minimal exercise** (Configuration) with a starter, a solution and tests, to prove the gates.
    The Configuration stub page describes it.
- Add **GitHub Actions** workflows, following ADR-0002:
  - deploy the site to GitHub Pages at `https://dotnet-primer.emmz.dev`, blocked by the site gates;
  - run the samples gates on pushes and pull requests.
- Add repo hygiene: `.gitignore`, `.editorconfig`.

Real content for each area comes in later changes. Diagrams and external link checking are deferred.

## Capabilities

### New Capabilities

- `site-structure`: the site's information architecture. Covers the introduction with drill-down links,
  subject areas, the learning-path order, standalone sections, per-area cheatsheets, and search.
- `content-page-standards`: what every content page carries: at least one Microsoft Learn link, a
  "Report a problem" issue link, and copyable code samples.
- `site-deployment`: how the site is published. Covers the custom domain over HTTPS, deploy on `main`,
  and the gates that block a bad deploy.
- `sample-exercises`: the companion exercise code. Covers the starter/solution/test layout, the pinned
  SDK, what must compile and what must pass, and how the site links to it.

### Modified Capabilities

_None. There are no existing specs._

## Impact

- **New top-level directories:** `site/` (Node/Astro project), `samples/` (.NET solution),
  `.github/workflows/`.
- **New root files:** `global.json`, `.gitignore`, `.editorconfig`.
- **New dependencies:** listed in `docs/dependencies.md`. Node ≥22.23, npm, .NET SDK 10.0.4xx, and
  Docker for sample tests.
- **External systems:** GitHub Pages (source set to Actions, custom domain), and Cloudflare DNS for
  `emmz.dev` (`CNAME dotnet-primer → emmz-makes-stuff.github.io`). Emmz configures both by hand.
- **Gates:** establishes the eight gate commands that `/emmz:setup` wraps in the Makefile.
