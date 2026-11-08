# Discovery — dotnet-primer

> Requirements only: *what* and *why*. Technology decisions are deferred to `/emmz:architecture`,
> except constraints confirmed as genuinely fixed (listed under *Hard constraints* with their source).

## Problem & users

- **Users:** a group of engineers who have worked on "essentially a .NET 3.5 codebase for 15 years".
- **Problem:** they need to get "up and running on .NET 10, ASP.NET Core, EF Core, gRPC and
  containers". Fifteen years on .NET 3.5 means a large gap in platform, idioms and tooling.
- **Why now:** a **migration is under way**. The .NET 3.5 codebase is being modernised, or is about to
  be, so the engineers need this soon.
- **How they'll use it:** both **self-paced learning** (reading through roughly in order) and
  **reference at the desk** (dipping in to look things up while working, cheatsheets especially).

## Outcomes & behaviours

- An **engaging static website** that brings these engineers up to speed. "Engaging" means:
  - **strong visual design**: a distinctive look, diagrams, good typography, a pleasure to read;
  - **copyable code samples**: syntax-highlighted, with one-click copy;
  - **exercises / hands-on**: the site describes each exercise, and a **companion sample repo**
    holds the starter and solution projects that engineers clone and run locally. The exercises are
    **for practice only, not for testing anyone**.
- **Learning path:** **ASP.NET Core is the primary use case** and the spine of the path. Other topics
  are introduced through it, **probably leading with Configuration, Dependency Injection and
  Middleware**.
- **Search:** site-wide search, "if possible". It supports the reference use.
- **Report a problem:** every page links to **open a GitHub issue** to report errors or omissions.
- **Framing:** topics teach **modern .NET on its own terms**. A **separate "coming from .NET 3.5"
  section** maps old habits to their modern equivalents (e.g. `web.config`/`ConfigurationManager` →
  `IConfiguration`).
- A **readable introduction** with links to **drill down** into specific subjects, e.g. Dependency
  Injection, Configuration.
- Pages carry **links to the relevant official Microsoft documentation** (`docs.microsoft.com` /
  Microsoft Learn).
- **Cheatsheets for each area**, e.g.:
  - common `dotnet` CLI commands;
  - patterns for registering services (DI);
  - logging, tracing, metrics;
  - "all that stuff should be covered".

## Scope

### In

Subject areas named so far:

- .NET 10 (the platform, the SDK and the `dotnet` CLI)
- ASP.NET Core
- EF Core
- gRPC
- Containers
- **C# language** as its own area (C# 3.0 → 14: async/await, nullable reference types, records,
  pattern matching, and so on)
- Cross-cutting: Dependency Injection, Configuration, Logging, Tracing, Metrics
- **Testing**: unit tests, integration tests (e.g. `WebApplicationFactory`), test containers
- **Async & performance**: async/await in depth, `Span<T>`, allocation awareness, benchmarking
- **Orchestration & cloud**: beyond containers, e.g. Kubernetes, .NET Aspire. Cloud-hosting guidance is
  **AWS-specific** (e.g. ECS, EKS, Lambda), because that is where the team deploys
- **Security & auth**: authentication and authorisation in ASP.NET Core, secrets management, JWT/OIDC
- A separate **"coming from .NET 3.5"** section mapping old to new
- The **companion sample code** for the exercises, kept **in this repo** alongside the site

### Explicitly out

- **Desktop / mobile UI**: WinForms/WPF on modern .NET, MAUI, Blazor.
- **Analytics / tracking**: no usage analytics or progress tracking on the site.
- **Porting guidance** for the actual .NET 3.5 codebase, e.g. the Upgrade Assistant. Not needed:
  "they'll be using agents" for the porting itself. The conceptual "coming from .NET 3.5" mapping stays
  in scope.
- **Assessment**: the exercises don't grade or test anyone.

### Later

_Nothing deferred yet._

## Hard constraints

- **Delivered as a static website.** Source: Emmz (stated requirement).
- **Hosted on GitHub Pages.** Source: Emmz (stated requirement).
- **Served on a custom domain via a Cloudflare CNAME.** Source: Emmz (stated requirement).
- **The subject matter is .NET 10 and its ecosystem.** This is the *content*, not a constraint on how
  the site is built.
- **The site is public.** Its content is generic .NET with nothing company-specific. Source: Emmz.

## Success criteria

- **Fewer basic questions:** the team stops asking Emmz the basics and looks them up on the site.
- **Used day to day:** cheatsheets are open in a tab while engineers work.
- **Positive feedback:** engineers say it was clear and enjoyable, and they finish the learning path.

Not chosen as a criterion: "engineers can build, test and containerise a new service unaided".

With analytics out of scope, these criteria are judged **qualitatively**, from team feedback and
Emmz's own observation, not from site metrics.

## Content upkeep

- Emmz's team writes and maintains the content, with a **periodic review**.
- Readers report errors or omissions through the **GitHub issue link** on each page.

## Open questions & ambiguities

- ~~**Search is "if possible".**~~ Resolved by ADR-0001: Starlight's built-in Pagefind index works on a
  static GitHub Pages site.
- **Path order beyond the opening:** the path starts with ASP.NET Core, leading with Configuration, DI
  and Middleware ("probably"). Where the remaining topics fall (EF Core, gRPC, logging/tracing/metrics,
  testing, security, async, containers, AWS) is still to be settled. The C# language area and the
  "coming from .NET 3.5" section are standalone, so where they sit relative to the path is also open.
- **Stale Microsoft docs links:** a periodic review is the agreed process. Whether to add an automated
  link check on top is undecided.
- **Later:** nothing has been deferred yet.
