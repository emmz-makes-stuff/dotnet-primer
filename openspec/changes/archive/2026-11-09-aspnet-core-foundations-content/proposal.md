# Proposal

## Why

The site is live, but every page on the learning path is still a "Coming soon" stub. The first four
pages are the ASP.NET Core entry page, then Configuration, Dependency Injection and Middleware. These
are the foundations every later topic builds on (logging, EF Core, gRPC and testing all assume them),
so engineers moving off .NET 3.5 need them first, and with depth.

## What Changes

- **ASP.NET Core entry page** (`/aspnet-core/`): the stub becomes a real page. It covers the generic
  host and `WebApplication`, the builder → build → run shape of `Program.cs`, minimal APIs, route
  groups and typed results, when to use controllers instead, and how the next three pages plug in. It
  gets no cheatsheet and no exercise.
- **Configuration page**: the stub becomes a full page. It covers:
  - providers and how they layer;
  - environments;
  - user secrets;
  - environment-variable naming;
  - `IConfiguration`;
  - the options pattern (`IOptions`, `IOptionsSnapshot`, `IOptionsMonitor`) and validation at startup;
  - pitfalls.

  The description of exercise 01 stays on the page, and the exercise itself is unchanged.
- **Dependency Injection page**: the stub becomes a full page. It covers lifetimes, registration
  patterns, primary-constructor injection, keyed services, scope validation and captive dependencies,
  and creating scopes outside a request. It also describes a new exercise 02.
- **Middleware page**: the stub becomes a full page. It covers:
  - the pipeline and why order matters;
  - `Use`/`Run`/`Map`;
  - the recommended order for built-in middleware;
  - writing custom middleware;
  - short-circuiting;
  - exception handling with Problem Details;
  - endpoint filters.

  It includes a diagram of the request pipeline and describes a new exercise 03.
- **Three new cheatsheets**: Configuration, Dependency Injection and Middleware. Each is linked from
  its topic page and from the Cheatsheets navigation.
- **Exercise 02 (dependency injection)** and **exercise 03 (middleware)**: new starter/solution/test
  sets under `samples/exercises/`, with the same layout and gates as exercise 01.
- **Diagrams**: the approach parked in the last change is settled here. Diagrams are hand-written SVG,
  themed with the site's colour tokens so they work in light and dark schemes, and each has a text
  alternative. The middleware pipeline is the first one.
- **Site test gate** gets three new checks:
  - pages delivered with full content carry no "Coming soon" placeholder;
  - every exercise folder is described and linked from a site page;
  - every diagram has a text alternative.

Out of scope:
- old-to-new mappings for these topics on the "Coming from .NET 3.5" page (that page stays a stub);
- content for any other learning-path page;
- the other parked items: learning-path order after Middleware, external link checking, the `/` search
  shortcut, and the duplicate home `<title>`.

## Capabilities

### New Capabilities

- `topic-page-content`: what a learning-path page with full content must contain and guarantee. That
  covers the subtopics each page covers, accurate .NET 10 code examples, pitfalls, no placeholder, and
  accessible diagrams.

### Modified Capabilities

- `site-structure`: "Cheatsheets per area" changes from delivering only the `dotnet` CLI cheatsheet to
  also delivering the Configuration, Dependency Injection and Middleware cheatsheets, with links in
  both directions.
- `sample-exercises`: "Site describes each exercise" gains scenarios for exercises 02 and 03, and a
  scenario that an undescribed exercise fails the site test gate.

## Impact

- **Site content**: `site/src/content/docs/aspnet-core/{index,configuration,dependency-injection,middleware}`
  (some may become `.mdx` to embed diagram components) and three new pages under
  `site/src/content/docs/cheatsheets/`.
- **Site code**:
  - a new diagram component under `site/src/components/`;
  - sidebar entries in `site/astro.config.mjs`;
  - cheatsheet cards on the introduction page;
  - new checks in `site/tests/dist.test.ts`.
- **Samples**:
  - `samples/exercises/02-dependency-injection/` and `samples/exercises/03-middleware/`;
  - entries in `samples/Primer.slnx` and `samples/Primer.Solutions.slnx`.
  - There are no new NuGet packages, because both exercises need only the ASP.NET Core shared framework
    and the existing test packages.
- **Dependencies**: no new tools, so `docs/dependencies.md` is unchanged.
- **CI**: no workflow changes. `site.yml` checks out the whole repo, so the site test can read
  `samples/exercises/`.
