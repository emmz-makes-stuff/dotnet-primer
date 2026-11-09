# Design

## Context

`bootstrap-site-and-samples` delivered the site skeleton. The first four learning-path pages are stubs
with a "Coming soon" note: `/aspnet-core/`, Configuration, Dependency Injection and Middleware.
Exercise 01 (configuration → options) exists with the starter/solution/shared-tests layout. The site
test gate (`site/tests/dist.test.ts`) checks the built `dist/`:
- the Learn block;
- the Report link;
- that internal links resolve;
- that the intro links every slug in `EXPECTED_SLUGS`;
- Pagefind;
- the CNAME.

Pages are plain `.md`, except the splash intro (`index.mdx`). Emmz's answers for this change:
- one cheatsheet per topic;
- new exercises 02 and 03, with 01 unchanged;
- hand-written SVG diagrams;
- the ASP.NET Core entry page is in scope;
- no 3.5 mappings in this change.

## Goals / Non-Goals

**Goals:**
- Pages an engineer can learn from on their own, using idiomatic .NET 10 code they can paste.
- A diagram approach that later changes reuse without rethinking it.
- Gate checks that stop the most likely regressions in content work: a forgotten placeholder, an
  exercise nobody links to, and a diagram with no text alternative.

**Non-Goals:**
- No compile-checking of page snippets in CI (see D5).
- No cheatsheet or exercise for the ASP.NET Core entry page.
- No changes to exercise 01's code. Only its page text around it changes.
- No restyling of the theme beyond what the diagram component needs.

## Decisions

### D1. Page format: `.md` by default, `.mdx` only to embed a component
The topic pages stay Markdown. The Middleware page becomes `middleware.mdx`, because it has to embed
the diagram component. Converting a page to `.mdx` keeps its URL, so links don't change. Any page
may use Starlight's built-in `Aside`, `Tabs` or `Steps` once it is MDX, but that is not a reason to
convert it on its own.
*Alternative rejected:* every page as MDX. It's noisier to author, and nothing else needs it.

### D2. Diagrams: inline SVG in an Astro component, coloured with Starlight tokens
Each diagram is an Astro component under `site/src/components/diagrams/` (first one:
`RequestPipeline.astro`), and it renders an inline `<svg>`.
- **Colours** come from CSS custom properties: `--sl-color-gray-*`, `--sl-color-accent*` and
  `--sl-color-text`. They follow Starlight's `data-theme` switch. A file loaded through `<img>` can't
  see page CSS, and `prefers-color-scheme` inside such a file ignores the site's manual toggle.
- **Accessibility:**
  - the `<svg>` has `role="img"` and `aria-labelledby` pointing at its `<title>` and `<desc>`;
  - the `<desc>` spells out the order (exception handler → HTTPS redirection → static files →
    routing → CORS → authentication → authorization → custom → endpoint) and the request-in /
    response-out flow;
  - all text is real `<text>` in the site's font, not paths, so it scales and Pagefind can index it.
- **Size:** a `viewBox` with `width: 100%`, so it shrinks on a phone. Labels stay at 14px or more,
  readable at a 360px width.
- **Gate:** a test asserts that every `svg[role="img"]` in a page's main content has a non-empty
  accessible name, through `aria-label` or the elements `aria-labelledby` points to. It also
  asserts that no content `<svg>` is missing `role="img"`, unless it is `aria-hidden`.
  - Starlight's own icons are `aria-hidden`, so they pass.
  - The assertion is scoped to `.sl-markdown-content`.

*Alternatives rejected:*
- Mermaid: needs Playwright at build time, and its output is harder to control in the theme.
- Committed `.svg` files: can't follow the theme toggle.

### D3. Page skeleton
Each topic page follows `topic-page-content` "Topic page structure":
1. An opening paragraph stating the outcome, then the cheatsheet link as a one-line `:::tip`.
2. Concept sections. Each `##` section is one subtopic from the coverage requirement, with code.
3. `## Pitfalls`, as a short list of mistakes, each with its symptom and its fix.
4. `## Exercise NN: …` (Configuration, Dependency Injection and Middleware only). This keeps the
   existing exercise-01 format: Goal, Where, Steps, Check your work, Stuck?

The entry page uses steps 1–3, replacing the cheatsheet tip with "where next" cards for the three
topic pages.

Pages teach modern .NET on its own terms. A one-sentence "you may know this as …" is fine where it
helps orientation, but old-to-new mappings belong on the coming-from-3.5 page (site-structure), which
is out of scope here.

### D4. Cheatsheets
They live at `cheatsheets/configuration.md`, `cheatsheets/dependency-injection.md` and
`cheatsheets/middleware.md`. Each follows the `dotnet-cli` cheatsheet's shape:
- `prev: false` and `next: false`, so they don't join the path's prev/next chain;
- a "Back to …" link first;
- `##` groups of copyable blocks, with minimal prose;
- its own `learn` links.

They are added to the sidebar's Cheatsheets group and to the intro page's "Getting started"-style
cards in a new "Cheatsheets" card grid. They are also added to `EXPECTED_SLUGS`, so the existing
intro-links test covers the new spec scenario. Content outline:
- **Configuration:**
  - the provider precedence table;
  - key ↔ env var ↔ command-line naming;
  - the `dotnet user-secrets` commands;
  - options registration (`Configure`, `AddOptions().Bind().ValidateDataAnnotations().ValidateOnStart()`);
  - choosing `IOptions*`.
- **Dependency Injection:**
  - a lifetime table;
  - registration one-liners (type, factory, instance, `TryAdd*`, `TryAddEnumerable`, open generic,
    keyed + `[FromKeyedServices]`);
  - injection sites (constructor, primary constructor, handler parameter, `[FromServices]`);
  - `IServiceScopeFactory` for background work.
- **Middleware:**
  - the recommended order;
  - `Use`/`Run`/`Map`/`UseWhen` shapes;
  - inline and class middleware templates;
  - the `OnStarting` header pattern;
  - `AddProblemDetails` + `UseExceptionHandler`;
  - an endpoint filter template.

### D5. Code accuracy: verify while writing, don't compile in CI
Snippets stay inline in Markdown. While authoring each page section, every snippet labelled as a
complete file is compiled in a scratch `dotnet new web` project, with `TreatWarningsAsErrors` and
`net10.0`, in the session scratchpad and not the repo. Fragments are checked against Microsoft Learn,
using the Microsoft Learn MCP search and fetch tools. The section's self-review records what was
compiled.
*Alternative rejected:* a `samples/snippets/` project imported into MDX through `?raw`. It's a real
guarantee, but it adds build coupling between the stacks and MDX everywhere, for a content set that
is reviewed by hand anyway. Revisit it if snippet drift shows up in "Report a problem" issues.

### D6. Exercise 02, dependency injection (`samples/exercises/02-dependency-injection/`)
Same layout as 01: `Starter`, `Solution`, `tests/` (shared), `Starter.Tests`, `Solution.Tests`. The
projects are `DependencyInjection.{Starter,Solution}[.Tests].csproj`, with root namespace
`DependencyInjection`.
- **App:**
  - `VisitCounter` (thread-safe `Interlocked.Increment`), which should be a singleton;
  - `RequestContext` (a `Guid Id` set at construction), which should be scoped;
  - `RequestContextReporter`, a transient that takes `RequestContext`.
- **Endpoints:**
  - `GET /visits` returns the next count;
  - `GET /request-context` returns JSON `{ handlerId, reporterId }`, one ID from the handler's
    injected `RequestContext` and one through `RequestContextReporter`.
- **Starter:** the classes exist. The `TODO`s are the three registrations and the two handlers' service
  parameters. Without registrations the endpoints can't resolve services. The handlers return 501
  until the TODOs are done, the same style as 01.
- **Tests** (shared, each with a message naming the behaviour that's missing):
  - three `/visits` calls return 1, 2, 3;
  - one `/request-context` call returns equal IDs;
  - two calls return different `handlerId`s.
- **Rejected:** testing lifetimes by resolving from `factory.Services`. That tests the container, not
  the engineer's app behaviour, and the spec says "through HTTP requests".

### D7. Exercise 03, middleware (`samples/exercises/03-middleware/`)
Projects are `Middleware.{Starter,Solution}[.Tests].csproj`, with namespace `Middleware`.
- **App:**
  - `AddProblemDetails()`;
  - `UseExceptionHandler()`, called unconditionally, so tests under `WebApplicationFactory` (which
    runs in Development) get Problem Details from the exception handler;
  - a provided `MaintenanceModeMiddleware`, which short-circuits with 503 when `Maintenance:Enabled`
    is true and ties back to exercise 01;
  - endpoints `GET /hello` (returns text) and `GET /boom` (throws).
- **Engineer's task:**
  1. Complete `CorrelationIdMiddleware` (a class with `InvokeAsync`). It reads `X-Correlation-Id`, or
     generates one, stores it in `HttpContext.Items`, and sets the response header through
     `Response.OnStarting`.
  2. Register it **before** `MaintenanceModeMiddleware`.
- **Why both points matter:** the starter registers the maintenance middleware first, so order matters
  for the 503 case. `OnStarting` matters for the 500 case, because the exception handler clears the
  response, headers included, before it writes Problem Details. A header set directly is lost
  wherever the middleware sits. The page's Pitfalls section explains both.
- **Tests:**
  - a header is generated;
  - an incoming header is echoed;
  - the header is present on the 503, using `WithWebHostBuilder` and an in-memory
    `Maintenance:Enabled=true`;
  - the header is present on the 500 with `application/problem+json`.

### D8. Gate additions in `site/tests/dist.test.ts`
1. **`FULL_CONTENT_SLUGS`:** the 4 pages and 3 cheatsheets of this change. None of them may contain
   the placeholder. That's detected by the text "Coming soon" inside an aside in
   `.sl-markdown-content`, matching how the stubs render `:::note[Coming soon]`. Later content changes
   extend the list.
2. **Exercises are linked:** read the folder names in `../samples/exercises/`, resolved from the test
   file. For each one, assert that some built page has an `<a>` whose href is exactly
   `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/<name>`. The test
   fails, naming the folder, if none does. This cross-stack read is acceptable because CI checks out
   the whole repo.
3. **Diagram accessibility,** as D2.
4. **Topic ↔ cheatsheet links:** a map of topic slug → cheatsheet slug, asserting a link in each
   direction. It includes the existing `platform/dotnet-10-and-cli` ↔ `cheatsheets/dotnet-cli` pair.
   That pair already links both ways, so it passes as it stands.

### D9. Section order
1. The diagram component and the gate additions. They go green on the existing stubs, because
   `FULL_CONTENT_SLUGS` starts empty and the cheatsheet map holds only the `dotnet-cli` pair.
2. ASP.NET Core entry page.
3. Configuration page and cheatsheet.
4. Exercise 02.
5. DI page and cheatsheet.
6. Exercise 03.
7. Middleware page, diagram and cheatsheet.

Each page section adds its slugs to `FULL_CONTENT_SLUGS` and the cheatsheet map in the same commit, so
the gate tightens as content lands. Each exercise section adds the exercise's description to its
(still stub) topic page, so the "exercise is linked" test is green at that commit too. The page
section that follows keeps the description and builds the page around it. Every section lands green.

## Risks / Trade-offs

- **The `Response.Clear()` behaviour behind D7's 500 case is from memory of the ASP.NET Core source.**
  Mitigation: exercise 03's solution tests prove it either way. If headers set directly do survive,
  the 500 scenario still holds, but the Pitfalls text changes. Record the finding in a devlog note.
- **Snippet drift without CI compile (D5)** → the compile-while-authoring step is recorded in each
  section note. "Report a problem" catches what slips through.
- **The placeholder detection depends on the text "Coming soon"** → acceptable. All stubs use exactly
  that aside title, and authoring guidance in `site/README.md` is updated to say so.
- **Long pages:** DI and Middleware could run long → keep each concept section short, with one example,
  and push the variations to the cheatsheet. Starlight's on-page TOC handles navigation.
- **Diagram legibility in both schemes needs a human check** → it's a stop-and-ask visual check
  (CLAUDE.md), with a preview recipe, in the Middleware section.
- **Microsoft Learn URLs move** → use canonical paths without locale or `?view=`, as in the last change.
  External link checking is still parked.
