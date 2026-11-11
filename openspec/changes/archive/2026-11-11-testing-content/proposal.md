# Proposal

## Why

Testing is the next page on the learning path, after gRPC, and it is still a placeholder. Emmz chose
it as the next change on 2026-11-10. The engineers come from .NET 3.5, where tests meant MSTest v1 or
NUnit 2, `[ExpectedException]`, static `DateTime.Now` and hard-to-fake dependencies, if there were
tests at all. The migration will need tests around every service it moves, and every exercise so far
already depends on xUnit v3, `WebApplicationFactory` and Testcontainers without explaining them.

## What Changes

- **Write the full Testing page** (`testing`), replacing the stub. It follows the stub's outline, plus
  code coverage, the extra Emmz picked on 2026-11-10 (not mutation testing, and no separate section
  on parallelism). She also picked snapshot testing, then dropped it after the §1.1 probe found that
  Verify now needs a paid licence for commercial use:
  - **Running tests:** xUnit v3 on Microsoft.Testing.Platform, `global.json`'s test runner, and
    `dotnet test`, including filtering.
  - **Writing tests:** `[Fact]`, `[Theory]` with data, assertions, async tests and
    `TestContext.Current.CancellationToken`.
  - **Fixtures and lifetime:** a new instance per test, constructor and `IDisposable` /
    `IAsyncLifetime`, and class, collection and assembly fixtures.
  - **Test doubles** (Emmz, 2026-11-10: both): hand-written fakes first, then Microsoft's
    `FakeTimeProvider` and `FakeLogger`, then **NSubstitute** for larger interfaces. Moq gets a
    sentence and no examples.
  - **Integration tests:** `WebApplicationFactory<Program>`, `ConfigureTestServices`, and replacing a
    dependency.
  - **Real dependencies:** Testcontainers, briefly, pointing at exercise 07 for practice.
  - **Code coverage** with the Microsoft.Testing.Platform coverage extension, and what a coverage
    number doesn't tell you.
  - **Coming from .NET 3.5:** a short table mapping MSTest v1 / NUnit 2 habits to today, after the
    concepts and before the Pitfalls, as the EF Core and gRPC pages do.
  - **Pitfalls,** then **Exercise 09**.
- **A Testing cheatsheet** (`cheatsheets/testing`): `dotnet test` commands, test shapes, fixtures,
  doubles, `WebApplicationFactory` and coverage. It is linked both ways with the page, listed
  in the sidebar and on the introduction, and added to the site tests' maps.
- **Exercise 09** (`samples/exercises/09-testing`) **turns the exercise pattern round**: the engineer
  writes the tests (Emmz, 2026-11-10: seeded bugs).
  - It ships a small, correct **orders minimal API** (Emmz: orders again, as in 07 and 08). It is in
    memory and needs no Docker.
  - It also ships **seeded bugs**: broken variants of the API, each catchable by one technique from
    the page:
    - a pure logic rule (a plain unit test);
    - a call to a dependency (a fake);
    - a time rule (`FakeTimeProvider`);
    - DI wiring (`WebApplicationFactory`).
  - The engineer's **Starter tests have one numbered `TODO` per technique** (Emmz, 2026-11-10).
  - **A checker** runs the engineer's tests against the correct API, then against each seeded bug. It
    reports which bugs their tests caught, and points at the `TODO` for each one they missed.
  - **The Solution tests** catch every seeded bug. They use NSubstitute for the warehouse double and
    `FakeTimeProvider` for time, as worked examples.
- **The samples gate checks exercise 09 itself:** the Solution tests must pass against the correct
  API, and must fail against every seeded bug. That proves the exercise can be completed, and stops a
  later edit from making a bug uncatchable.
- **The samples README:** the layout and the exercise table gain 09, with its different shape.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- **`topic-page-content`:**
  - "Full-content pages carry no placeholder" adds the Testing page and its cheatsheet to its
    "Every finished page is checked" scenario.
  - New: "Testing page covers running and writing tests", "… covers test doubles", "… covers
    integration tests and real dependencies", "… covers code coverage", and "Testing page maps
    .NET 3.5 habits".
- **`site-structure`:** "Cheatsheets per area" grows from nine cheatsheets to ten.
- **`sample-exercises`:**
  - "Exercise layout" allows a testing exercise, whose starter and solution are test projects for an
    app that comes already written.
  - "Practice, not assessment" says how a testing exercise reports progress: through the checker,
    as caught and missed bugs.
  - "Samples gates" requires the solution tests of a testing exercise to catch every seeded bug.
  - "Site describes each exercise" gains a scenario for exercise 09.
  - New: "Testing exercise seeds a bug for each technique", and "Testing exercise checker reports
    caught and missed bugs".

## Impact

- **Site:**
  - `site/src/content/docs/testing/index.md`, and a new `site/src/content/docs/cheatsheets/testing.md`;
  - the sidebar's Cheatsheets group in `site/astro.config.mjs`;
  - the cheatsheet cards on `site/src/content/docs/index.mdx`;
  - `site/tests/dist.test.ts` (`EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS`, `CHEATSHEETS`).
- **Samples:**
  - `samples/exercises/09-testing`, and both `.slnx` files;
  - new `PackageVersion` entries in `samples/Directory.Packages.props` for `NSubstitute` and
    `Microsoft.Extensions.TimeProvider.Testing` (versions fixed in §1);
  - the samples test gate in the `Makefile` (and the `samples` CI workflow, if it doesn't call the
    Makefile) gains the exercise-09 checker run (design D5);
  - `samples/README.md`.
- **Page-only package:** the coverage extension (`Microsoft.Testing.Extensions.CodeCoverage`) is
  shown on the page and checked in a scratch project. No exercise uses it, so it doesn't go in
  `Directory.Packages.props`.
- **Tooling:** none new. The checker runs on the pinned .NET SDK, so `docs/dependencies.md` and
  `make deps` don't change.
- **Out of scope:**
  - mutation testing tools (Stryker.NET);
  - snapshot testing (Verify needs a paid licence for commercial use; Emmz, 2026-11-10);
  - a separate section on parallelism (xUnit's parallel collections appear only where fixtures need
    them);
  - Moq examples, beyond a sentence;
  - MSTest and NUnit today, beyond a sentence each (ADR-0003 picked xUnit v3);
  - benchmarking, which belongs to the Async & performance page;
  - UI and browser testing (Playwright);
  - a new Testcontainers exercise: exercise 07 already practises it.
