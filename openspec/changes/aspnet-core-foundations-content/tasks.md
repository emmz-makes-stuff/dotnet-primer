# Tasks

## 1. Diagram component and gate additions

- [x] 1.1 Add `site/src/components/diagrams/RequestPipeline.astro` (design D2). It is an inline SVG
  coloured with `--sl-color-*` tokens, has `role="img"`, `<title>` and `<desc>` describing the
  recommended order and the request/response flow, a `viewBox` and `width: 100%`, and labels of
  ≥14px. Verify that `make site-build` and `make site-lint` pass with the component not yet used
  on any page.
- [x] 1.2 Add the diagram accessibility test to `site/tests/dist.test.ts` (design D8.3). It checks that
  every `svg[role="img"]` in `.sl-markdown-content` has a non-empty accessible name, and that no
  content `<svg>` lacks `role="img"` unless it is `aria-hidden`. Verify it by temporarily rendering a
  nameless SVG in a page: the test fails and names the page. Then revert and confirm that
  `make site-test` is green.
- [x] 1.3 Add the "every exercise folder is linked" test (design D8.2). It reads
  `samples/exercises/*` and requires an exact GitHub `tree/main/samples/exercises/<name>` link on
  some built page. Verify that it passes for `01-configuration`, and that it fails and names the
  folder when a dummy folder is added. Remove the dummy afterwards.
- [x] 1.4 Add `FULL_CONTENT_SLUGS` (empty for now) with the no-placeholder test, and the topic ↔
  cheatsheet link map seeded with `platform/dotnet-10-and-cli` ↔ `cheatsheets/dotnet-cli` (design
  D8.1, D8.4). Verify the placeholder test by temporarily listing a stub slug: it fails and names
  the page. Revert. Then `make site-test` must be green.
- [x] 1.5 Update `site/README.md` (authoring guide) to cover:
  - the page skeleton (D3);
  - the cheatsheet shape (D4);
  - the diagram component pattern (D2);
  - adding a finished page to `FULL_CONTENT_SLUGS` and the cheatsheet map;
  - the fact that the placeholder is detected by the "Coming soon" aside title.

  Verify that `make site-format` passes.

## 2. ASP.NET Core entry page

- [x] 2.1 Rewrite `site/src/content/docs/aspnet-core/index.md` to cover the host and `WebApplication`,
  the builder → build → run shape, minimal API endpoints with route parameters, route groups,
  `TypedResults`, and when to use controllers. End with a Pitfalls section and "where next" links to
  Configuration, DI and Middleware (spec: ASP.NET Core entry page coverage). Keep or refresh the
  `learn` links.
- [x] 2.2 Compile every complete-file snippet on the page in a scratch `dotnet new web` project
  (net10.0, warnings as errors) in the session scratchpad. Check the fragments against Microsoft
  Learn. Record what was compiled in the section-done note.
- [x] 2.3 Add `aspnet-core` to `FULL_CONTENT_SLUGS`. Verify with `make gates`.

## 3. Configuration page and cheatsheet

- [x] 3.1 Create `site/src/content/docs/cheatsheets/configuration.md` (design D4), with a back link to
  the Configuration page and `learn` links. Add it to the sidebar Cheatsheets group, to a new
  "Cheatsheets" card grid on `index.mdx`, and to `EXPECTED_SLUGS`.
- [x] 3.2 Rewrite `site/src/content/docs/aspnet-core/configuration.md` per D3 to cover every item in
  "Configuration page coverage":
  - providers and precedence;
  - environments;
  - user secrets;
  - `__` and command-line naming;
  - `IConfiguration`;
  - the options pattern;
  - `IOptions` vs `IOptionsSnapshot` vs `IOptionsMonitor`;
  - `ValidateOnStart`.

  Add the Pitfalls section, put the cheatsheet tip at the top, and keep the existing exercise 01
  description (it already links to the exercise).
- [x] 3.3 Compile and check the snippets as in 2.2, and record the results.
- [x] 3.4 Add `aspnet-core/configuration` and `cheatsheets/configuration` to `FULL_CONTENT_SLUGS`, and
  the pair to the cheatsheet link map. Verify with `make gates`.

## 4. Exercise 02: dependency injection

- [x] 4.1 Scaffold `samples/exercises/02-dependency-injection/`, mirroring 01 (design D6):
  - `DependencyInjection.Starter` and `DependencyInjection.Solution` web projects;
  - the shared `tests/`;
  - the `Starter.Tests` and `Solution.Tests` projects.

  Add the starter and solution projects with their test projects to `samples/Primer.slnx`, and only
  the solution with its tests to `samples/Primer.Solutions.slnx`. Verify with `make samples-build`.
- [x] 4.2 Implement the Solution (`VisitCounter` singleton, `RequestContext` scoped,
  `RequestContextReporter` transient, `GET /visits`, `GET /request-context`) and the shared tests
  (counts 1/2/3; same IDs within a request; different `handlerId` across requests). Each assertion
  message must name the missing behaviour. Verify that `make samples-test` passes.
- [x] 4.3 Write the Starter with `TODO`s for the three registrations and the handler service
  parameters, and with handlers returning 501. Verify that
  `dotnet test --project samples/exercises/02-dependency-injection/Starter.Tests` fails with the
  guiding messages. Then complete the TODOs in a scratch copy, confirm the tests go green, and
  discard the copy.
- [x] 4.4 Add the "Exercise 02" description to the DI page, in the exercise-01 format: goal, where
  (with the exact GitHub tree link), steps, check your work, and "stuck?". Verify that the
  exercise-linked test passes with `make gates`.

## 5. Dependency Injection page and cheatsheet

- [x] 5.1 Create `site/src/content/docs/cheatsheets/dependency-injection.md` (D4), and wire it into
  the sidebar, the intro cards and `EXPECTED_SLUGS`.
- [x] 5.2 Rewrite `site/src/content/docs/aspnet-core/dependency-injection.md` per D3 to cover every
  item in "Dependency Injection page coverage":
  - registration;
  - the three lifetimes, each with its lifespan and a typical use;
  - constructor, primary-constructor and handler injection;
  - interface → implementation, factory, multiple implementations, `TryAdd`, open generics, keyed
    services;
  - captive dependencies and scope validation in Development;
  - `IServiceScopeFactory` outside a request.

  Add the Pitfalls section and the cheatsheet tip, and keep the exercise 02 section from 4.4 at the
  end.
- [x] 5.3 Compile and check the snippets as in 2.2. Include a complete captive-dependency example, and
  confirm that it throws at startup in Development. Record the results.
- [x] 5.4 Add the page and cheatsheet slugs to `FULL_CONTENT_SLUGS`, and the pair to the cheatsheet
  link map. Verify with `make gates`.

## 6. Exercise 03: middleware

- [x] 6.1 Scaffold `samples/exercises/03-middleware/` like 4.1 (design D7), with projects
  `Middleware.{Starter,Solution}[.Tests]`, and add them to both `.slnx` files the same way. Verify
  with `make samples-build`.
- [x] 6.2 Implement the Solution:
  - `AddProblemDetails`;
  - an unconditional `UseExceptionHandler()`;
  - the provided `MaintenanceModeMiddleware` (503 when `Maintenance:Enabled`);
  - `CorrelationIdMiddleware` using `Response.OnStarting` and `HttpContext.Items`, registered before
    maintenance mode;
  - `GET /hello` and `GET /boom`.

  Write the shared tests: header generated, header echoed, header on the 503 (in-memory config
  override), and header on the 500 `application/problem+json`. Verify that `make samples-test`
  passes.
- [x] 6.3 Prove the D7 risk either way. In a scratch copy of the Solution, set the header directly
  instead of through `OnStarting`, run the 500 test, and record in a devlog note whether it fails
  (`Response.Clear()` wipes headers). Adjust the planned Pitfalls wording to match.
- [x] 6.4 Write the Starter:
  - `CorrelationIdMiddleware` with `TODO`s;
  - the pipeline with maintenance mode registered first;
  - a `TODO` to register the correlation middleware in the right place.

  Verify that the Starter tests fail with guiding messages, that they go green on a completed scratch
  copy, and discard the copy.
- [x] 6.5 Add the "Exercise 03" description to the Middleware page, in the exercise-01 format. Verify
  the exercise-linked test with `make gates`.

## 7. Middleware page, diagram and cheatsheet

- [x] 7.1 Create `site/src/content/docs/cheatsheets/middleware.md` (D4), and wire it into the
  sidebar, the intro cards and `EXPECTED_SLUGS`.
- [x] 7.2 Convert the Middleware page to `middleware.mdx` (D1) and rewrite it per D3 to cover every
  item in "Middleware page coverage":
  - the pipeline, with the `RequestPipeline` diagram;
  - `Use`/`Run`/`Map`;
  - the recommended order, with the reasons routing precedes authentication and authorization;
  - inline and class middleware;
  - short-circuiting;
  - Problem Details exception handling;
  - endpoint filters vs middleware.

  The Pitfalls section must include the `OnStarting` lesson from 6.3. Add the cheatsheet tip, and
  keep the exercise 03 section at the end. Verify that the URL `/aspnet-core/middleware/` is
  unchanged in `dist/`.
- [x] 7.3 Compile and check the snippets as in 2.2, and record the results.
- [x] 7.4 Add the page and cheatsheet slugs to `FULL_CONTENT_SLUGS`, and the pair to the cheatsheet
  link map. Verify with `make gates`. Check that the diagram accessibility test sees the diagram (it
  should count at least one `svg[role="img"]` on the Middleware page).
- [x] 7.5 Human check (stop and ask). Emmz previews the site with
  `npm --prefix site run build && npm --prefix site run preview`, and checks:
  - all 7 pages and the diagram in light and dark modes, including switching with the theme toggle;
  - the diagram at phone width;
  - copy buttons on the cheatsheets;
  - a search for "keyed services" and for "OnStarting".

  Record her answer in a devlog note.

## 8. Review fixes: samples README

- [x] 8.1 Update `samples/README.md` for exercises 02 and 03:
  - the Layout tree shows all three exercise folders;
  - "Working an exercise" says which site page describes each exercise;
  - the Docker note says exercises 01–03 don't need Docker.

## Workflow follow-up

- Whole-change review, the change-done note, and the offer of `code-review`, per CLAUDE.md
  "Finishing a change".
- Merge to `main` and push, which deploys the site, then check the live pages.
- Offer `/opsx:archive`.
