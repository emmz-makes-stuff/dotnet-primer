# Stopped: human visual check (task 7.5)

<!-- Section: 7 · Tasks: 7.1–7.5 · Base: e050cd0 -->

Tasks 7.1–7.4 are implemented, but **uncommitted and unticked**, per the CLAUDE.md stop procedure.
Task 7.5 needs Emmz to look at the pages in a browser.

## What's in the working tree

- **`site/src/content/docs/cheatsheets/middleware.md`** (new) covers:
  - the recommended order;
  - `Use`/`Run`/`Map`/`UseWhen` shapes;
  - a middleware class template;
  - short-circuit and `.ShortCircuit()`;
  - the `OnStarting` header pattern;
  - Problem Details plus an `IExceptionHandler` template;
  - endpoint filters;
  - a middleware vs filter table.
- **`aspnet-core/middleware.md` → `middleware.mdx`,** via `git mv`, with the URL unchanged. It imports
  `RequestPipeline` and covers:
  - the pipeline, with the diagram;
  - `Use`/`Run`/`Map`;
  - the recommended order, with reasons and the real "contains authorization metadata" error;
  - what `WebApplication` adds automatically, checked against Learn: the dev exception page, routing,
    authentication and authorization, and user middleware between routing and endpoints;
  - class middleware, with the constructor-singleton vs `InvokeAsync`-scoped rule;
  - short-circuiting;
  - Problem Details;
  - endpoint filters, with a when-to-use table;
  - 6 pitfalls, including the `OnStarting` / `Response.Clear()` lesson from 6.3.

  The exercise 03 section is at the end.
- **Wiring:** the sidebar, the intro Cheatsheets grid, `EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS` and
  `CHEATSHEETS`.

## Verified so far

- **Compiled,** then probed against a real Kestrel:
  - `Use`/`Map` give `/ping` → `pong`;
  - the full order example;
  - class middleware with a scoped `InvokeAsync` parameter returns 200 twice in Development, with no
    scope error;
  - short-circuit: `/orders` without a key gives 401, and `/health` with `.ShortCircuit()` gives 200
    without a key;
  - errors: `/orders/7` gives 404 problem+json "Order not found", `/boom` gives 500 problem+json, and
    `/nope` gives 404 problem+json through `UseStatusCodePages`;
  - filter: quantity 0 gives 400 validation problem, and 2 gives 201;
  - authorization before routing gives 500
    `Endpoint HTTP: GET /orders contains authorization metadata, but a middleware was not found that supports authorization.`;
  - all cheatsheet C#, combined into one file.
- **Built from compiled fragments** using the template method. All 6 are verbatim after Prettier.
- **Two CORS claims softened** to "can turn away… preflight", because I hadn't proven a rejection.
- **In `dist/`:**
  - one `svg[role="img"]` in the Middleware content, labelled by its `<title>` and `<desc>` (the desc
    lists the 9 stages in order);
  - `#pitfalls` exists, which the exercise section links to;
  - `/aspnet-core/#route-groups` exists, which the endpoint-filters section links to.
- **Learn links:** 4 OK.

Gates on the uncommitted tree: `GATES_EXIT:0`. The full set was `SITE_BUILD_EXIT:0`,
`SITE_LINT_EXIT:0`, `SITE_FORMAT_EXIT:0`, `SITE_TEST_EXIT:0`, `SAMPLES_BUILD_EXIT:0`,
`SAMPLES_FORMAT_EXIT:0`, `SAMPLES_TEST_EXIT:0` and `VALIDATE_EXIT:0`.

## The question for Emmz

Do the 7 pages and the request-pipeline diagram look right in light and dark modes? That includes
switching with the theme toggle, the diagram at phone width, copy buttons on the cheatsheets, and
search results for "keyed services" and "OnStarting".

## Next

- **If she approves:** tick 7.1–7.5, write the section-7-done note quoting her answer, and commit §7.
  Then do the whole-change review (`git diff 812d803`… no: the first base of *this* change is
  `f629fdb`, so use `git diff f629fdb..HEAD`), write the change-done note, and offer `code-review`.
- **If she wants changes:** they belong to §7 if they're small, such as diagram colours, sizes or
  wording. Make them, rebuild, and ask again. Anything bigger becomes a new section after agreement.
- **The preview** must run unsandboxed, because Astro writes outside the sandbox. Stop it with
  `npx astro preview stop` or by killing its PID, never by port.
