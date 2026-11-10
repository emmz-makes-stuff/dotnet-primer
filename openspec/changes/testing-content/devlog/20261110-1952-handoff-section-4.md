# Handoff: §4 half built, uncommitted; 4.3 (verify the page) in progress

<!-- Section: 4 · Tasks: 4.1–4.2 done (ticked, uncommitted), 4.3 in progress · Base: 8b7e429 -->

The context reached 40% in the middle of 4.3. **Nothing in §4 is committed.** The section commits
once, after 4.4 and the gates.

## Uncommitted files and their state

- `site/src/content/docs/cheatsheets/testing.md`: **new**, the 4.1 cheatsheet (design D9). It has a
  back link and three Learn links. In 4.3 I split its blocks so that each one is a compilable
  fragment:
  - the `[assembly: AssemblyFixture]` line is in its own block (an assembly attribute can't follow
    types);
  - the fake class and the statements after it are separate blocks;
  - `WithWebHostBuilder` is its own block;
  - `ApiTests` and `OrderQueryTests` now have bodies that read their constructor parameter
    (otherwise CS9113).
- `site/astro.config.mjs`: `cheatsheets/testing` is in the sidebar's Cheatsheets group.
- `site/src/content/docs/index.mdx`: a Testing card in the introduction's Cheatsheets grid.
- `site/tests/dist.test.ts`: `cheatsheets/testing` is in `EXPECTED_SLUGS`. **It isn't yet in
  `FULL_CONTENT_SLUGS` or `CHEATSHEETS`;** that's 4.4.
- `site/src/content/docs/testing/index.md`: the full page (4.2), per D1 and D8, formatted with
  prettier. Its sections:
  - Running tests, Writing tests, Fixtures and lifetime;
  - Test doubles: a hand-written fake, fake time, fake logging, NSubstitute and a Moq sentence;
  - Integration tests, Real dependencies, Code coverage;
  - Coming from .NET 3.5, Pitfalls, and Exercise 09.
- `samples/exercises/07-ef-core/tests/SqlServerFixture.cs`: its remark said the Testing pages were
  "still to come". It now links `https://dotnet-primer.emmz.dev/testing/`. This is a small
  follow-on outside the task list, because that remark is now false.
- `openspec/changes/testing-content/tasks.md`: 4.1 and 4.2 ticked.

At the last check, the gates gave `SITE_BUILD_EXIT:0`, `SITE_FORMAT_EXIT:0` and `SITE_LINT_EXIT:0`.
That was before the cheatsheet split, so they need running again. `SITE_TEST_EXIT` hasn't been run
since the page was written: the 09 link check should now pass.

## Real outputs already on the page

These come from a scratch copy of exercise 09 with the coverage package added. The copy is at
`/private/tmp/claude-501/-Users-rendle-github-emmz-dotnet-primer/08944cf2-a4a7-4c2b-9845-eb4eb7cd0859/scratchpad/page/`,
but treat it as gone after a clear.
- `--filter-class TestingOrders.Tests.PricingTests` gives `total: 4`, `succeeded: 4`.
- `--coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` gives
  "In process file artifacts produced: …/TestResults/coverage.cobertura.xml" and `total: 13`.
- The first element of the Cobertura report: `line-rate="0.6923076923076923"
  branch-rate="0.6956521739130435" lines-covered="81" lines-valid="117" branches-covered="32"
  branches-valid="46"`.

## Fixes already made to the page draft

- In the 3.5 table, `[RowTest]` became `[TestCase]` (NUnit 2.5) or `[DataSource]` (MSTest), because
  RowTest was MbUnit.
- The exit-8 pitfall's unverified "non-public methods" claim is gone.
- The coverage claim is reworded so it doesn't say "almost as much".
- The `FakeTimeProvider` sentence now names `Task.Delay(delay, timeProvider)`.
- The anchors are fixed to `/observability/logging/#exercise-04-logging-an-order-endpoint` and
  `/data/ef-core/#exercise-07-a-database-for-the-orders-app`.

## Gotchas

- `npx prettier` must run from inside `site/`. From the repo root it fails with "Cannot find
  package 'prettier-plugin-astro'".
- Claude Code's safety check blocks `rm -rf "$VAR"/…`. Use `"${VAR:?}"/…` or a literal path. When
  it's blocked, the whole command doesn't run.
- Background `dotnet run` doesn't survive. For a live server, build first, then
  `(dotnet bin/Debug/net10.0/X.dll > log &)`, and call it with `node -e` and `fetch`.

## Next

- **Resume 4.3:** compile every C# block of both pages verbatim, with warnings as errors.
  - Extract the ```` ```csharp ```` blocks with a small node script: the page had 14 and the
    cheatsheet 5 before the split, so the cheatsheet has more now.
  - Wrap each one in a scratch test project that references a copy of the exercise 09 App and has
    the packages it needs: xunit.v3, Mvc.Testing, TimeProvider.Testing, Diagnostics.Testing,
    NSubstitute (+Analyzers) and Testcontainers.MsSql.
  - Wrappers: a namespace per block; a class with the fields a member-level or statement-level
    block uses (`_lines`, `_orders`, `_time`, `_client`, `ct`); and stubs for the cheatsheet's
    hypothetical "Shop" types (`OrderLine.Validate()`, `IPriceList`, `SeedAsync`,
    `DatabaseFixture`).
  - **Check by running:** that `FakeLogger<LoggingWarehouse>` on `ReleaseAsync(42, two lines)` gives
    `LatestRecord.Message == "Released stock for order 42: 2 lines"`, as the page claims.
  - Confirm the "from the exercise" snippets match the real files (`PricingTests`,
    `CancelWindowTests`, the NSubstitute tests, `OrdersApiFactory`, `PlacedOrderCanBeReadBack`,
    `CancelPolicy.CanCancel`, and the shortened `SqlServerFixture`, which the page marks as
    from 07). They are deliberately trimmed in places: say which in the note.
  - Check every URL on both pages with `fetch` (in `ctx_execute` or `node -e`). The 09 tree link
    will 404 until it's on `main`, as before.
  - Record all of it in the section-done note.
- Then **4.4**: add `testing` and `cheatsheets/testing` to `FULL_CONTENT_SLUGS`, and
  `"testing": "cheatsheets/testing"` to `CHEATSHEETS`. Then `make gates`, which should now give
  `GATES_EXIT:0`, because the page links 09.
- Then the §4 section-done note and one commit:
  `feat(testing-content): testing cheatsheet and page (§4)`.
- Deferred: 6.1's human check reviews the checker's hint wording.
- Nothing is running in the background. The App server from §2 and the background task are both
  stopped. The branch is pushed up to `8b7e429`, with 0 commits unpushed.
