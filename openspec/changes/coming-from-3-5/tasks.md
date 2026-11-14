# Tasks

## 1. Probe: session across two in-process hosts

- [x] 1.1 In a scratch tree with the repo's `global.json`, `.editorconfig` and
  `samples/Directory.Build.props`, build a minimal session app and two `WebApplicationFactory`
  hosts sharing one `MemoryDistributedCache` (D7). Record, with exact output:
  - whether host B reads host A's session cookie with default Data Protection and distinct content
    roots (it should not);
  - that it does once both share a keys directory and application name;
  - that a recording cache sees the basket's JSON inside the bytes session writes.

  **If host B reads the cookie without shared keys, stop and ask** before the test design changes.
  Write a devlog note.
- [x] 1.2 In the same tree, record for the page (D8):
  - two concurrent requests to one session that each add an item, losing one write, with the delay
    that makes it repeatable;
  - that the Redis example compiles against `Microsoft.Extensions.Caching.StackExchangeRedis`.

  Fetch Learn's session and state, distributed caching and Data Protection pages, and list the URLs
  the page will cite. Write a devlog note.

## 2. Exercise 15: session and state

- [x] 2.1 Scaffold `samples/exercises/15-session-state/` (D6): `SessionState.Starter` and
  `SessionState.Solution` web apps, a shared `tests/`, and `Starter.Tests` and `Solution.Tests`. Add
  them to `Primer.slnx`, and the Solution and its tests to `Primer.Solutions.slnx`. Verify with
  `make samples-build`.
- [x] 2.2 Write the Solution: the endpoints, price list and `OrderStore` from D6, with TODOs 1–4
  solved. Verify that it builds with warnings as errors.
- [x] 2.3 Write the tests (D7): one behaviour per test, each failure naming what's wrong, its TODO
  and the page's path. Verify that the Solution passes ten runs in a row.
- [x] 2.4 Write the Starter with `ServerBaskets` and TODOs 1–4. Verify that:
  - it builds clean;
  - each test fails against it with its guiding message;
  - a scratch copy completed with the Solution's code passes everything;
  - scratch copies that each leave out one TODO fail only that TODO's tests.
- [x] 2.5 Push the branch and run the `samples` workflow on it
  (`gh workflow run samples.yml --ref change/coming-from-3-5`). Check that it passes on
  `ubuntu-latest` with exercise 15's tests, and record the run ID in the section-done note.
  - `SITE_TEST_EXIT:1` is expected here (D9), because no page links exercise 15 yet. Record it as a
    deliberate red.

## 3. The Session and state page and its cheatsheet

- [x] 3.1 Write `site/src/content/docs/aspnet-core/session-and-state.md`: the outcome, the cheatsheet
  link, turning on session, typed JSON access, the missing lock with §1's real run, session across
  replicas (distributed cache, Redis, Data Protection, linking to the Security page's section),
  moving state out of session, "Coming from .NET 3.5", Pitfalls, and Exercise 15 (needs no Docker
  and no Redis, with the exact GitHub tree link). Add it to the sidebar after Middleware, to
  `EXPECTED_SLUGS` and `FULL_CONTENT_SLUGS`, and to the introduction. Verify that `make site-build`
  and `make site-test` pass, and that Middleware's next and Logging's previous are Session and
  state.
- [x] 3.2 Create `site/src/content/docs/cheatsheets/session-and-state.md`, with the back link and
  Learn links. Add it to the sidebar's Cheatsheets group, the introduction, `EXPECTED_SLUGS`,
  `FULL_CONTENT_SLUGS` and `CHEATSHEETS`. Verify that `make site-test` passes.
- [x] 3.3 Verify the page and cheatsheet (D8): compile every C# block, check every output against a
  real run, and fetch every URL. Verify that `make gates` shows `GATES_EXIT:0`, and record it in the
  section-done note.

## 4. Back-filled "Coming from .NET 3.5" sections

- [x] 4.1 Add a "Coming from .NET 3.5" section, per D4, to the ASP.NET Core, Configuration,
  Dependency Injection and Middleware pages, each Modern cell linking to its own page's heading.
  Verify that `make site-build` passes and every new link resolves.
- [x] 4.2 Add the same to the Logging, Tracing and Metrics pages, and the `netTcpBinding` row to the
  gRPC page's "Coming from WCF" table. Verify that `make site-build` passes and every new link
  resolves.
- [x] 4.3 Fetch every Learn URL the new rows cite. Verify that `make gates` shows `GATES_EXIT:0`, and
  list each section added in the section-done note.

## 5. The hub and its cheatsheet

- [x] 5.1 Add the D5 tests to `site/tests/dist.test.ts`: the hub links every learning-path page's
  "Coming from" section, and every fragment link on the hub and its cheatsheet resolves. Verify
  that both fail against the placeholder, naming what's missing.
- [x] 5.2 Rewrite `site/src/content/docs/coming-from-3-5/index.md` per D2: the outcome, the cheatsheet
  link, the grouped tables with a sentence per group, and the Learn links. Keep `prev: false` and
  `next: false`, and add it to `FULL_CONTENT_SLUGS`. Verify that the 5.1 tests pass.
- [x] 5.3 Create `site/src/content/docs/cheatsheets/coming-from-3-5.md` per D3. Add it to the
  sidebar, the introduction, `EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS` and `CHEATSHEETS`. Verify that
  `make site-test` passes.
- [x] 5.4 Update `samples/README.md`: exercise 15 in the layout, the exercise table and the list of
  exercises that need no Docker. Verify that `make gates` shows `GATES_EXIT:0`.

## 6. Human check

- [ ] 6.1 Run exercise 15 from a clean clone myself (the Starter fails with guiding messages, and the
  Solution passes), and report the result. Then stop for Emmz to check, with a copy-paste recipe:
  - the hub, its cheatsheet, the Session and state page and its cheatsheet, in light and dark
    schemes, including the hub's three-column tables at phone width;
  - one back-filled section, and following a hub link to it;
  - search finds "Session";
  - Middleware → Session and state → Logging with the previous and next links.

  On PASS, tick and continue. On FAIL, add a review-fix section.

## Workflow follow-up

- Self-review the whole change, write the change-done note, and offer the `code-review` skill.
- Check that the main specs still hold, for example "Exercise 07's and exercise 12's are the only
  tests that need Docker".
- Keep each requirement's text under 500 characters before archiving, and run
  `openspec validate --specs --strict` after the sync.
- Merge to `main` with Emmz's go-ahead, check the live site and the tree link, then offer
  `/opsx:archive`.
