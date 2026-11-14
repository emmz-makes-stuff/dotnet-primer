# Design

## Context

- **The placeholder** `coming-from-3-5/index.md` has `prev: false` and `next: false`, two Learn
  links ("Port from .NET Framework to .NET", "Migrate from ASP.NET to ASP.NET Core"), a paragraph
  and the "Coming soon" note. It sits in its own sidebar group, and the introduction links it under
  standalone sections. It isn't in `FULL_CONTENT_SLUGS` and has no cheatsheet.
- **Existing "Coming from" sections** (found 2026-11-13):
  - "Coming from .NET 3.5" on Testing, Security & auth, Async & performance, Containers and
    Orchestration & AWS;
  - "Coming from WCF" on gRPC, and "Coming from Entity Framework 6" on EF Core;
  - "Coming from C# 3" on the three C# themed pages.

  Each one is a two-column table of old thing and modern thing. The CLI page has prose about the
  release cadence instead. The first seven learning-path pages have none.
- **Nothing on the site covers session.** The Security page's "Data protection" section covers the
  key ring and its AWS provider. The Async & performance page covers `HybridCache` and mentions
  `AddStackExchangeRedisCache`.
- **Learning-path pagination comes from the sidebar order**, so adding Session and state after
  Middleware in `astro.config.mjs` updates Middleware's next and Logging's previous by itself.
- **The site tests** (`site/tests/dist.test.ts`) need every `EXPECTED_SLUGS` entry linked from the
  introduction, no placeholder on `FULL_CONTENT_SLUGS`, the `CHEATSHEETS` pairs linked both ways,
  and every exercise folder linked from some page. They don't check heading anchors yet.
- **Exercises with a web host** use `WebApplicationFactory<Program>` from
  `Microsoft.AspNetCore.Mvc.Testing`, which `Directory.Packages.props` already pins. Session,
  `MemoryDistributedCache` and Data Protection are all in the ASP.NET Core shared framework.
- **Decisions Emmz made** (2026-11-13, explore session): see proposal.md. In short: shape B (a hub
  that links out, plus back-filled sections), the MVC, Web API and WCF-over-net.tcp stack, a new
  Session and state page with exercise 15, and the hub grouped by the old thing's area.

## Goals / Non-Goals

**Goals:**
- An engineer can start from something in the old codebase, find it on the hub in a few seconds,
  and land on the section that explains its replacement.
- Every learning-path page reads the same way: concepts, then "Coming from", then Pitfalls.
- Exercise 15's tests show the failure a real scale-out causes (a basket that vanishes on the other
  replica) without Docker or Redis.

**Non-Goals:**
- Teaching porting. The hub maps concepts; it doesn't say how to move a project file.
- Covering every System.Web API. The rows are the ones an MVC, Web API and WCF codebase uses.
- Testing the per-session lock in the exercise. It's timing-dependent, so the page shows it from a
  real run and the exercise doesn't test it.

## Decisions

### D1. Session and state is on the learning path, after Middleware

Session is middleware (`UseSession`) and builds on DI and configuration, so it reads naturally
after Middleware and before Observability. Slug `aspnet-core/session-and-state`, title "Session and
state". It's on the path rather than in the hub because it teaches new material, not just a
mapping. Exercise 15 keeps its number: exercises are numbered in the order they were written, not
path order.

*Alternative:* a section on the ASP.NET Core page. Emmz chose a page.

### D2. The hub's groups and row format

Groups, in this order: web.config and hosting; MVC and Web API; WCF; session and state; data;
diagnostics (logging, tracing, metrics); threading and async; testing; security; deployment
(containers and orchestration). Each group is a three-column table: **.NET 3.5**, **Modern**, and
**Read** (a link to the topic page's section). A row's Modern cell is a phrase, not code where a
phrase will do; the topic page holds the detail.

Rows come from the topic pages' "Coming from" tables, so the hub never says something a topic page
doesn't. Where a row is on two pages (for example `HttpContext.Current`), it links to the one that
explains it most fully.

*Alternative:* grouping by page in sidebar order. Emmz chose grouping by the old thing's area, so a
reader starts from what they know.

### D3. The hub cheatsheet is the hub's tables, compressed

`cheatsheets/coming-from-3-5` keeps the same groups, with two columns (**.NET 3.5**, **Modern**)
where the Modern cell is a link to the topic section. The hub adds a sentence of context per group
and the third column; the cheatsheet doesn't. The `CHEATSHEETS` map pairs `coming-from-3-5` with it.

### D4. Back-filled sections match the existing ones

Each new "Coming from .NET 3.5" section is a two-column table (**.NET 3.5**, **Modern**), placed
after the concepts and before Pitfalls, as on the Testing page. Each Modern cell links to the
heading on the same page that explains it. Rows (from the explore session):

| Page | Rows |
| --- | --- |
| ASP.NET Core | `Global.asax` `Application_Start`; `System.Web.Mvc.Controller` and `ApiController`; IIS and `System.Web`; `RouteConfig` and `WebApiConfig` |
| Configuration | `<appSettings>`; `<connectionStrings>`; `ConfigurationManager`; `Web.Release.config` transforms; custom `ConfigurationSection` |
| Dependency Injection | static singletons; `DependencyResolver.SetResolver` and `GlobalConfiguration.Configuration.DependencyResolver`; Unity, Autofac or Ninject wiring; `HttpContext.Current.Items` per-request state |
| Middleware | `IHttpModule`; `Application_BeginRequest` and `EndRequest`; Web API `DelegatingHandler`; MVC's and Web API's separate filters; `IHttpHandler` |
| Logging | log4net or Enterprise Library; `Trace.WriteLine`; string-formatted messages |
| Tracing | correlation ids passed by hand; WCF message logging and `System.Diagnostics` trace listeners |
| Metrics | Windows performance counters; `PerformanceCounter` in code |

The gRPC page's WCF table gains `netTcpBinding` → gRPC over HTTP/2.

### D5. Hub links are checked, anchors included

A new dist test reads every learning-path page's HTML, finds its `h2` whose id starts with
`coming-from-`, and checks that the hub links to `/<slug>/#<id>`. A second test resolves every
`#fragment` link on the hub and its cheatsheet against the target page's heading ids. Ids come from
the built HTML, so they match whatever github-slugger made.

### D6. Exercise 15's app

`samples/exercises/15-session-state`, the usual layout: `SessionState.Starter`,
`SessionState.Solution`, a shared `tests/`, `Starter.Tests` and `Solution.Tests`. A minimal API with:

- `POST /basket/items` (`{ sku, quantity }`), `GET /basket`, `POST /checkout` (returns the order id
  and total), and `GET /orders/{id}`;
- a small fixed price list, and an `OrderStore` registered as a singleton.

The Starter keeps baskets in a `ServerBaskets` singleton (a dictionary keyed by a `basket-id`
cookie that the app sets) and keeps the placed order in that dictionary too. A singleton, not a
static, so that two hosts in one test process don't share it by accident, just as two servers
wouldn't. TODOs:

1. Turn on session (`AddDistributedMemoryCache`, `AddSession`, `UseSession`), and remove
   `ServerBaskets` and the `basket-id` cookie.
2. Write `GetJson<T>` and `SetJson<T>` extension methods on `ISession`, and use them for the basket.
3. Configure Data Protection from configuration: `PersistKeysToFileSystem` with
   `DataProtection:KeysPath`, and `SetApplicationName` with `DataProtection:ApplicationName`.
4. At checkout, save the order to `OrderStore`, clear the basket, and keep only the order id in
   session.

### D7. Exercise 15's tests

- **Two instances** are two `WebApplicationFactory<Program>` hosts. Each test gives both one shared
  `MemoryDistributedCache` (replacing `IDistributedCache` in `ConfigureTestServices`), one temporary
  keys directory and one application name through configuration. Each host gets a different content
  root, as two servers have, so that without TODO 3 their default Data Protection isolation makes
  the other instance's cookie unreadable. §1's probe confirms that this fails without TODO 3 and
  passes with it. The distinct roots only stand in for two servers. Replicas of one container image
  all share one path (`/app`), and they lose session because each one makes its own key ring. The
  Session and state page explains that real reason, and the exercise's description says that the
  tests fake it with two content roots.
- **Cookies are handled by the tests**, not the client (`HandleCookies = false`), so a test can carry
  one instance's session cookie to the other.
- **JSON in session:** the shared cache is wrapped in a recorder, and the test checks that a value
  the app wrote contains the basket's JSON. Session's own format stores each value's bytes as they
  are, so the JSON shows up inside it; §1 confirms this.
- Each failure names what's wrong, its TODO and the page path, as in the other exercises. No Docker,
  no Redis, no network.

### D8. Examples are real

Every code block on the Session and state page compiles on the pinned SDK with the repo's settings.
The concurrent-requests example shows output from a real run. The Redis example compiles against `Microsoft.Extensions.Caching.StackExchangeRedis` in a
scratch project; the page says it needs that package, and the exercise doesn't use it. Every URL is
fetched before the section lands.

### D9. Section order

The exercise first, then the page that describes it, as in earlier changes: the site's
"every exercise is linked" test goes red when the exercise folder lands, and green when the page
does. That red is deliberate, and recorded in §2's note and commit. Then the back-fill, then the
hub, which needs every section to exist to link to it.

## Risks / Trade-offs

- [Two hosts in one process might share Data Protection keys through the user profile, so the
  cross-instance test passes without TODO 3] → D7's distinct content roots, confirmed by §1's probe.
  If the probe shows they still share, stop and ask before changing the test design.
- [Hub rows drift from the topic pages as pages change] → D5's tests catch a missing section or a
  broken anchor. They can't catch a reworded row, so D2 keeps the hub's cells short and the detail
  on the topic pages.
- [The lost-write demo is timing-dependent] → it's on the page from a real run, with the delay that
  makes it repeatable stated, and not in the exercise's tests.
- [Adding a learning-path page changes Middleware's next and Logging's previous] → the existing
  pagination test covers path order; the new scenario checks Session and state's links.
