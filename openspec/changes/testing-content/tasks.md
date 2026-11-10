# Tasks

## 1. Probe and package versions

- [ ] 1.1 Probe the setup (design D4, D5, D6, D10). In scratch projects on 10.0.4xx:
  - a web app with one file swapped by an MSBuild property from an imported `.targets` file, and
    an xUnit v3 test project that references it. Run `dotnet test --project … -p:SeededBug=1` with a
    separate artifacts path, and check that the swapped file is compiled, that the default build is
    unaffected afterwards, and that two such runs don't share `obj/`;
  - Microsoft.Testing.Platform's exit codes for pass, a failed test, and no tests (0, 2 and 8?);
  - which report file xUnit v3 can write with no extra package, and how to read failed test names
    from it;
  - the exact `dotnet test` filter syntax for one class, and one test, on Microsoft.Testing.Platform;
  - the coverage extension's command and output file, and Verify's first run and mismatch;
  - how long a run of five sequential `dotnet test` builds takes.

  Write a devlog note with the exact commands, exit codes, output and timings, and revise design D4–D6
  if anything differs.
- [ ] 1.2 Add `PackageVersion` entries for `NSubstitute`, `NSubstitute.Analyzers.CSharp` and
  `Microsoft.Extensions.TimeProvider.Testing` to `samples/Directory.Packages.props`, at the newest
  stable versions. Verify that `make samples-build` and `make samples-format` pass (unsandboxed, for
  NuGet). **If restore reports a vulnerable transitive package, stop and ask.**

## 2. Exercise 09: the API, the bugs and the Solution tests

- [ ] 2.1 Scaffold `samples/exercises/09-testing/` per design D2:
  - `Testing.App`, `Testing.Starter.Tests`, `Testing.Solution.Tests` and `Testing.Checker`;
  - `bugs/SeededBugs.targets`, imported by the App.

  Add all of them to `Primer.slnx`, and only the App and the Solution tests to
  `Primer.Solutions.slnx`. Verify with `make samples-build`.
- [ ] 2.2 Implement the App per D3: the three endpoints, `Pricing`, `CancelPolicy`, `OrderService`,
  `IWarehouse` with its logging implementation, `OrderRegistration` and validation. Verify that
  `make samples-build` passes, and hit each endpoint once by hand on a local run.
- [ ] 2.3 Write `Testing.Solution.Tests` per D7: the four files, with NSubstitute, `FakeTimeProvider`
  and a `WebApplicationFactory<Program>` subclass. Verify that `make samples-test` passes.
- [ ] 2.4 Write the four seeded bugs per D3, each a copy of one App file with one marked change.
  Verify each by hand with `dotnet test --project Solution.Tests -p:SeededBug=n`: at least one test
  fails per bug, and it's the test for that bug's technique.

## 3. Exercise 09: the checker and the gate

- [ ] 3.1 Write `Testing.Checker` per D5:
  - the drift check of each bug file against its App file;
  - the correct-API run first;
  - one run per bug, with progress output, the caught/missed report and the hints;
  - exit codes.

  Verify every scenario of "Testing exercise checker reports caught and missed bugs" by hand:
  - Solution tests: all caught, exit 0;
  - Starter tests as shipped: no tests, all missed;
  - a scratch copy of the Solution with one test deliberately broken: reported as wrong, bugs not
    run;
  - a scratch copy with the TODO 3 test removed: only bug 3 missed, with its hint;
  - a scratch edit that drifts a bug file: the drift is reported.

  Discard the scratch copies.
- [ ] 3.2 Write the Starter tests per D7: four files with numbered `TODO`s and no tests. Complete a
  scratch copy with tests of my own (not copied from the Solution), confirm that the checker reports
  all four caught, and discard it.
- [ ] 3.3 Add the checker run to the samples test gate (D6): the `Makefile`'s `samples-test`, the
  `samples` CI workflow step, and the `make samples-test` line in CLAUDE.md's Gates list. Verify that
  `make gates` shows `GATES_EXIT:0`, and that temporarily deleting one Solution test makes
  `make samples-test` fail and name the bug (then restore it).
- [ ] 3.4 Push the branch, and check that the `samples` workflow passes on `ubuntu-latest` with the
  checker run. Record the run ID and the checker's duration in the section-done note.

## 4. Testing cheatsheet and page

- [ ] 4.1 Create `site/src/content/docs/cheatsheets/testing.md` (design D9), with a back link and
  Learn links. Add it to the sidebar's Cheatsheets group, the introduction's cheatsheet cards and
  `EXPECTED_SLUGS`. Verify that `make site-build` passes.
- [ ] 4.2 Rewrite `site/src/content/docs/testing/index.md` per D1 and D8. Cover every item in the six
  Testing page requirements. Add:
  - the cheatsheet tip;
  - the Pitfalls (including reading `DateTime.Now` in code under test, shared state between tests,
    and treating coverage as proof);
  - an "Exercise 09" section in the exercise-01 format. It says that the engineer writes the tests,
    that it needs no Docker, how to run the checker, and gives the exact GitHub tree link.
- [ ] 4.3 Verify the page (D10):
  - compile every snippet in a scratch project, with warnings as errors;
  - run the filter, coverage and Verify examples, and paste their real output;
  - confirm that the "from the exercise" snippets match the real files.

  Record all of it in the section-done note.
- [ ] 4.4 Add `testing` and `cheatsheets/testing` to `FULL_CONTENT_SLUGS`, and the pair to
  `CHEATSHEETS`. Check every Learn link with the link script. Verify that `make gates` shows
  `GATES_EXIT:0`.

## 5. Samples README

- [ ] 5.1 Update `samples/README.md`: add 09 to the layout and the exercise table. Describe its
  different shape (the app is written, you write the tests, run the checker), and keep the Docker
  prerequisite saying that only exercise 07 needs it. Verify with `make gates`.

## 6. Human check

- [ ] 6.1 Run exercise 09 from a clean clone myself, and report the result:
  - the checker on the Starter reports all bugs missed;
  - on the Solution, all caught;
  - the timings.

  Then stop for Emmz to check, with a copy-paste recipe:
  - the Testing page and its cheatsheet, in light and dark schemes;
  - search finds "WebApplicationFactory";
  - the code-block copy button on a C# block;
  - the checker's output and hint wording, from running it on the Starter.

  On PASS, tick and continue. On FAIL, add a review-fix section.

## Workflow follow-up

- Self-review the whole change, write the change-done note, and offer the `code-review` skill.
- Merge to `main` and push, with Emmz's go-ahead, then check the live page and cheatsheet.
- Offer `/opsx:archive`. After the sync, run `openspec validate --specs --strict`.
