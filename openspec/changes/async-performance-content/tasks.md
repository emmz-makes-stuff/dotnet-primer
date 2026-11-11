# Tasks

## 1. Probe, tools and package versions

- [ ] 1.1 **Stop and ask Emmz first** whether to add `dotnet-counters` and `dotnet-trace` (design
  D5, proposal Impact). On a yes:
  - install both with `dotnet tool install -g` (unsandboxed);
  - add their rows to `docs/dependencies.md`, and their checks to `make deps`;
  - say why in a devlog note.

  Verify with `make deps` showing `DEPS_EXIT:0`. On a no, rewrite D5 and the starvation
  requirement's diagnostics with Emmz before going on.
- [ ] 1.2 Probe the exercise's test approach (design D3), in a scratch copy of the shape D2
  describes:
  - that a cancelled `HttpClient` request through `TestServer` cancels `RequestAborted`, and that
    the fake warehouse sees it;
  - the barrier test for concurrent `/quote` calls: it passes with `Task.WhenAll`, and fails with
    sequential awaits;
  - the fake logger collector catching the background worker's Error entry;
  - **the IL check for blocking:** finding the handler's `MethodInfo` in endpoint metadata,
    following `AsyncStateMachineAttribute` to `MoveNext`, and resolving `call`/`callvirt` tokens.
    Run it against four handlers:
    - synchronous, with `.GetAwaiter().GetResult()`;
    - `async`, with `.Result` inside;
    - `async`, with `.Wait()`;
    - correct.

    The first three must fail, each naming the call, and the last must pass. **If the check can't
    be made to work, stop and ask** whether to fall back to the timing test (design D3).

  Write a devlog note with the results.
- [ ] 1.3 Probe the page's claims (design D5, D6, D11), in scratch projects on 10.0.4xx:
  - `dotnet-counters` against `/blocking` and `/async` under the same load: the counter names .NET
    10 prints, and the values;
  - `dotnet-trace collect` on the blocking run, and whether `--format speedscope` or
    `dotnet-trace report` names the blocking frame;
  - an `async void` crash in a console app: its real output;
  - a client disconnect mid-request: the exception the server sees;
  - `HybridCache`: the current package version, the factory call count for 50 concurrent requests,
    `RemoveAsync`, and whether `RemoveByTagAsync` works with the in-memory level alone;
  - output caching: how a cached response shows (the `Age` header, or a handler counter), and
    that an authenticated request isn't cached;
  - the current `BenchmarkDotNet` version, and whether its analyzers or ours clash with
    `latest-recommended`.

  Write a devlog note with the exact outputs and versions.
- [ ] 1.4 Add the `BenchmarkDotNet` `PackageVersion` (design D9) to `samples/Directory.Packages.props`.
  Verify that `make samples-build` and `make samples-format` pass (unsandboxed, for NuGet). **If
  restore reports a vulnerable package, stop and ask.**

## 2. Exercise 11: async

- [ ] 2.1 Scaffold `samples/exercises/11-async/` like 10 (design D2): `Async.Starter` and
  `Async.Solution` web projects, a shared `tests/`, and `Starter.Tests` and `Solution.Tests`. Add
  them to `Primer.slnx`, and the Solution and its tests to `Primer.Solutions.slnx`. Verify with
  `make samples-build`.
- [ ] 2.2 Write the Solution: the store, the validation, `IWarehouse` and its `Task.Delay` version,
  the `NotificationQueue` and its `BackgroundService`, and both endpoints with TODOs 1–4 solved.
  Verify by running it and calling both endpoints.
- [ ] 2.3 Write the tests (design D3): the test app with a controllable fake warehouse, the IL
  reader for the blocking check, and one test per bug. No test asserts on elapsed time. Each failure
  names its TODO. Verify that the Solution passes, three runs in a row.
- [ ] 2.4 Write the Starter, with the four bugs and TODOs 1–4 (design D2). Verify that each test
  fails against it with its guiding message, and that completing a scratch copy with the
  Solution's code passes everything. Then complete another scratch copy that leaves out each TODO
  in turn, and confirm that only that TODO's test fails.
- [ ] 2.5 Push the branch, and run the `samples` workflow on it
  (`gh workflow run samples.yml --ref change/async-performance-content`). Check that it passes on
  `ubuntu-latest` with exercise 11's tests, and record the run ID in the section-done note. One run
  is enough, because no test depends on timing.
  - `SITE_TEST_EXIT:1` is expected here (D10). Record it as a deliberate red.

## 3. Benchmark project

- [ ] 3.1 Create `samples/benchmarks/Benchmarks` (design D4), in `Primer.slnx` only, with the five
  benchmark classes, a baseline and `[MemoryDiagnoser]` in each, and `BenchmarkSwitcher`. Relax only
  the analyzer rules BenchmarkDotNet needs, and say which in the section note. Verify that
  `make samples-build` and `make samples-format` pass, and that `make samples-test` doesn't run it.
- [ ] 3.2 Run it in Release on this Mac, with `--filter '*'`. Keep the full result tables and the
  environment block for the page. Check that `git status` shows no artifacts outside an ignored
  `BenchmarkDotNet.Artifacts/` folder, and ignore it if needed.
  - `SITE_TEST_EXIT:1` is still expected (D10).

## 4. Async & performance cheatsheet and page

- [ ] 4.1 Create `site/src/content/docs/cheatsheets/async-and-performance.md` (design D8), with a
  back link and Learn links. Add it to the sidebar's Cheatsheets group, the introduction's
  cheatsheet cards and `EXPECTED_SLUGS`. Verify that `make site-build` passes.
- [ ] 4.2 Rewrite `site/src/content/docs/performance/async-and-performance.md` per D1, D5, D6 and
  D7. Cover every item in the six Async page requirements. Add:
  - the cheatsheet tip;
  - the Pitfalls, including:
    - `.Result` or `.Wait()` on a task;
    - a token that's taken but not passed down;
    - `async void` outside event handlers;
    - a discarded task;
    - a span kept across an `await`;
    - claiming a speed-up from a `Stopwatch` loop, or a Debug build;
    - output caching that silently skips authenticated requests;
  - an "Exercise 11" section in the exercise-10 format. It says that it needs no Docker, and gives
    the exact GitHub tree link.
- [ ] 4.3 Verify the page (D11):
  - compile every snippet in a scratch project, with warnings as errors;
  - check that every number on the page comes from 1.3's or 3.2's real output;
  - confirm that the "from the exercise" snippets match the real files.

  Record all of it in the section-done note.
- [ ] 4.4 Add `performance/async-and-performance` and `cheatsheets/async-and-performance` to
  `FULL_CONTENT_SLUGS`, and the pair to `CHEATSHEETS`. Fetch every URL on both pages. Verify that
  `make gates` shows `GATES_EXIT:0`.

## 5. Samples README

- [ ] 5.1 Update `samples/README.md`: add 11 and `benchmarks/` to the layout, 11 to the exercise
  table, and how to run the benchmarks (Release, `--filter`, and that the gates don't run them).
  Keep the Docker prerequisite saying that only exercise 07 needs it. Verify with `make gates`.

## 6. Human check

- [ ] 6.1 Run exercise 11 from a clean clone myself (the Starter fails with guiding messages, and
  the Solution passes), and report the result. Then stop for Emmz to check, with a copy-paste recipe:
  - the Async & performance page and its cheatsheet, in light and dark schemes, including the wide
    benchmark tables;
  - search finds "ValueTask";
  - the code-block copy button on a C# block.

  On PASS, tick and continue. On FAIL, add a review-fix section.

## Workflow follow-up

- Self-review the whole change, write the change-done note, and offer the `code-review` skill.
- Merge to `main` and push, with Emmz's go-ahead, then check the live page and cheatsheet.
- Offer `/opsx:archive`. Before moving the change, run `openspec validate --specs --strict` on the
  synced specs.
