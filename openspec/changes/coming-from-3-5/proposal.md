# Proposal

## Why

"Coming from .NET 3.5" is the last placeholder on the site. DISCOVERY.md asks for a section that
maps old habits to modern ones. Eight topic pages already have a "Coming from" table, but the first
seven pages on the learning path don't, and they're the ones the team meets first. The team's
codebase is MVC, Web API and WCF over net.tcp, and it uses System.Web's `Session` heavily. Nothing
on the site covers session yet, and it's one of the harder things to move to containers with
several replicas.

## What Changes

The scope, agreed with Emmz in an explore session on 2026-11-13 (shape B):

- **The hub.** `coming-from-3-5/` stops being a placeholder. It becomes a table grouped by the area
  of the old thing: web.config and hosting, MVC and Web API, WCF, session and state, data,
  diagnostics, threading and async, testing, security, and deployment. Each row gives the old thing and its modern equivalent,
  and links to the "Coming from" section on the topic page that explains it. Its rows come from the
  existing eight sections and the new ones below.
- **A hub cheatsheet** (`cheatsheets/coming-from-3-5`): the same mappings, compressed, linked both
  ways with the hub.
- **Back-filled "Coming from .NET 3.5" tables** on the seven pages that lack one, with rows for the
  team's MVC and Web API stack:
  - ASP.NET Core: `Global.asax`, `System.Web.Mvc.Controller` and `ApiController` as two stacks, IIS
    hosting → `Program.cs`, one `ControllerBase` or minimal APIs, Kestrel;
  - Configuration: `<appSettings>`, `<connectionStrings>`, `ConfigurationManager`, config transforms
    → `IConfiguration`, options, environment variables;
  - Dependency Injection: static singletons, MVC's and Web API's separate dependency resolvers,
    Unity, Autofac and Ninject wiring → the built-in container and lifetimes;
  - Middleware: `IHttpModule`, Web API's `DelegatingHandler`, MVC's and Web API's separate filters →
    middleware and one filter pipeline;
  - Logging: log4net, Enterprise Library, `Trace.WriteLine` → `ILogger` and structured logging;
  - Tracing: hand-rolled correlation ids, WCF message logging → `Activity` and OpenTelemetry;
  - Metrics: Windows performance counters → `Meter`.
- **The gRPC page's "Coming from WCF" table** gains a `netTcpBinding` row.
- **A new learning-path page, "Session and state"** (`aspnet-core/session-and-state`), straight
  after Middleware. It covers:
  - `ISession`, and that it holds only strings and bytes, so objects are serialized;
  - that it has no per-user lock, so concurrent requests can overwrite each other;
  - session across replicas, with a distributed cache such as Redis and a shared Data Protection
    key ring;
  - moving state out of session: what belongs in a database, a cookie, claims or the client.

  It has its own "Coming from .NET 3.5" table, Pitfalls, a cheatsheet, and exercise 15.
- **Exercise 15** (`samples/exercises/15-session-state`): a small basket-and-checkout API whose
  Starter keeps each user's basket in per-server memory, the way InProc session did. Its TODOs move
  the basket into typed `ISession` access, make it work across two app instances, and move the
  placed order out of session. The tests run two in-process instances that share one distributed
  cache, so they need no Docker.
- **The introduction and the samples README** gain the new page, its cheatsheet, the hub cheatsheet
  and exercise 15.

**Out of scope:**
- porting guidance, the .NET Upgrade Assistant and step-by-step migration recipes (DISCOVERY.md:
  the team will port with agents);
- WebForms, ASMX and `System.Web` adapters (the team's stack is MVC, Web API and WCF);
- SignalR, and `TempData` beyond a line in the session page's table;
- a running Redis in the exercise or the gates.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- **`site-structure`:**
  - "One page per subject area" adds Session and state;
  - "Learning path with ASP.NET Core as the spine" puts Session and state after Middleware;
  - "Cheatsheets per area" grows from fifteen cheatsheets to seventeen;
  - new: "Coming from .NET 3.5 hub".
- **`topic-page-content`:**
  - "Full-content pages carry no placeholder" covers the hub, the Session and state page and both
    new cheatsheets;
  - "gRPC page maps WCF habits" adds `netTcpBinding`;
  - new: "Learning-path pages map .NET 3.5 habits", "Hub rows link to the topic pages", and the
    Session and state page's coverage (`ISession`, across replicas, moving state out, and its
    mapping).
- **`sample-exercises`:**
  - "Site describes each exercise" gains exercise 15;
  - new: requirements for exercise 15's three parts.

## Impact

- **Site:**
  - `site/src/content/docs/coming-from-3-5/index.md`, rewritten, and the new
    `aspnet-core/session-and-state.md`, `cheatsheets/session-and-state.md` and
    `cheatsheets/coming-from-3-5.md`;
  - a new "Coming from .NET 3.5" section on `aspnet-core/index.md`, `configuration.md`,
    `dependency-injection.md`, `middleware.mdx`, and `observability/logging.md`, `tracing.md` and
    `metrics.md`, and a row on `services/grpc.md`;
  - the sidebar's Learning path and Cheatsheets groups in `site/astro.config.mjs`;
  - `site/src/content/docs/index.mdx`, and `site/tests/dist.test.ts`.
- **Samples:** `samples/exercises/15-session-state`, both `.slnx` files, and `samples/README.md`.
  It needs no new packages beyond what ASP.NET Core ships (`Microsoft.AspNetCore.Mvc.Testing` is
  already used by other exercises).
- **Gates:** `make samples-test` gains exercise 15's tests, which need no Docker.
- **Tooling:** nothing new.
