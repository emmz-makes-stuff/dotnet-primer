# Tasks

## 1. Probe and package versions

- [ ] 1.1 Probe the setup (design D3, D4, D5, D8). In a scratch `dotnet new web` app on 10.0.4xx,
  unsandboxed:
  - Add `Microsoft.EntityFrameworkCore.SqlServer` and `.Design` at 10.0.14, with a two-entity model.
  - Run `dnx dotnet-ef@10.0.14 migrations add InitialCreate` and `database update` against a SQL
    Server container started with the page's `docker run` command. **If `dnx` can't run
    `dotnet-ef`, stop and ask** before using the manifest fallback.
  - Confirm the image runs on this Mac under OrbStack (amd64 under Rosetta). Record the start time,
    and the newest `2025-CU*-ubuntu-24.04` tag to pin.
  - In a scratch xUnit v3 project, start `MsSqlContainer` (Testcontainers.MsSql 4.16.0) with that
    tag, and check `Database.HasPendingModelChanges()` and a `DbCommandInterceptor` count.
  - Check the current Apple silicon switches against each tool's docs: Rancher Desktop (VZ and
    Rosetta), Colima (`--vm-type vz --vz-rosetta`) and Docker Desktop. Check Azure SQL Edge's status.
  - Stop and remove the containers.
  - Write a devlog note with the exact commands, tags, preference names and timings.
- [ ] 1.2 Add the three agreed `PackageVersion` entries to `samples/Directory.Packages.props`.
  Verify that `make samples-build` and `make samples-format` pass (unsandboxed, for NuGet).

## 2. Exercise 07: EF Core

- [ ] 2.1 Scaffold `samples/exercises/07-ef-core/` like 04–06 (design D4):
  - `EfCore.Starter` and `EfCore.Solution` web projects;
  - the shared `tests/`;
  - `Starter.Tests` and `Solution.Tests`.

  Add them to `Primer.slnx`. Add only the Solution and its tests to `Primer.Solutions.slnx`. Verify
  with `make samples-build`.
- [ ] 2.2 Implement the Solution:
  - `OrdersDbContext`, the `Order`/`OrderLine` entities, and their `IEntityTypeConfiguration`
    classes;
  - the three D4 endpoints;
  - the `InitialCreate` migration, generated with `dnx dotnet-ef` and reviewed.

  Verify that `make samples-build` passes.
- [ ] 2.3 Write the shared tests (D4): the assembly fixture with the pinned image and the
  Docker-missing message, a database per test class, and the `SaveChanges` and command
  interceptors.
  - Cover every scenario in the three exercise-07 requirements, with assertion messages that name
    the missing behaviour.
  - Verify that `make samples-test` passes (unsandboxed). Run it twice more to check that it's
    stable.
  - Stop Docker, run the tests, and check the Docker-missing message.
- [ ] 2.4 Write the Starter:
  - registration and the configuration classes as `TODO`s, and no `Migrations/` folder;
  - a save that works but calls `SaveChanges` per line;
  - an N+1 list that works;
  - a cancel `TODO`.

  Verify that the Starter tests fail with the guiding messages. Complete a scratch copy, including
  `dnx dotnet-ef migrations add`, confirm that its tests go green, and discard it.
- [ ] 2.5 Push the branch, and check that the `samples` workflow passes on `ubuntu-latest` with
  exercise 07's tests run (D8). Record the run ID in the section-done note.

## 3. EF Core cheatsheet and page

- [ ] 3.1 Create `site/src/content/docs/cheatsheets/ef-core.md` (design D6), with a back link and
  Learn links. Add it to the sidebar's Cheatsheets group, the introduction's cheatsheet cards and
  `EXPECTED_SLUGS`. Verify that `make site-build` passes.
- [ ] 3.2 Rewrite `site/src/content/docs/data/ef-core.md` per D1 and D2. Cover every item in the five
  EF Core page requirements. Add the cheatsheet tip, the Pitfalls (including "don't test with the
  in-memory provider"), and an "Exercise 07" section in the exercise-01 format. That section says
  the tests need Docker, links the Docker page, and gives the exact GitHub tree link.
- [ ] 3.3 Verify the page (D7):
  - compile every snippet in a scratch project (warnings as errors);
  - paste every SQL listing from EF Core's own log, run against the container;
  - run the concurrency example and the production migration commands;
  - confirm that the "from the exercise" snippets match the real files.

  Record all of it in the section-done note.
- [ ] 3.4 Add `data/ef-core` and `cheatsheets/ef-core` to `FULL_CONTENT_SLUGS`, and the pair to
  `CHEATSHEETS`. Check every Learn link with the link script. Verify that `make gates` shows
  `GATES_EXIT:0`.

## 4. Docker page, samples README and dependencies

- [ ] 4.1 Add the Apple silicon note to the Docker page's `## macOS` (design D5), with the switches
  checked in 1.1, and add EF Core to the opening list of pages that need Docker. Verify with
  `make gates`, and check that the built page has the note under the macOS heading.
- [ ] 4.2 Update `samples/README.md`:
  - add 07 to the layout and the exercise table;
  - the Docker prerequisite says exercise 07's tests need it;
  - the line "Exercises 01–06 don't need it" still holds.

  Add "exercise 07's tests" to the docker row's "Used for" in `docs/dependencies.md`. Verify with
  `make gates` and `make deps`.

## 5. Human check

- [ ] 5.1 Stop for Emmz to check, with a copy-paste recipe:
  - the EF Core page and its cheatsheet, in light and dark schemes;
  - search finds "DbContext";
  - the code-block copy button on a `dnx dotnet-ef` command;
  - exercise 07 run from a clean clone (Starter fails with guiding messages, Solution passes).

  On PASS, tick and continue. On FAIL, add a review-fix section.

## Workflow follow-up

- Whole-change review, the change-done note, and the offer of `code-review`, per CLAUDE.md
  "Finishing a change". Check the review against the **main** specs too, not just the delta specs.
- Merge to `main` and push, which deploys the site, then check the live page.
- Offer `/opsx:archive`.
