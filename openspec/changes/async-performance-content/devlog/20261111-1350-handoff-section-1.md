# Handoff: §1 built to 1.3, uncommitted; 1.4 (BenchmarkDotNet version) next

<!-- Section: 1 · Tasks: 1.1–1.3 done (ticked, uncommitted), 1.4 open · Base: 1f98ddf -->

The context passed 40% after 1.3. **Nothing in §1 is committed.** The section commits once, after
1.4 and the gates.

This note's stamp, and those of `1330` and `1345`, run ahead of the clock (it's 13:22). The names
keep the order. A new note must sort after `1350`.

## Uncommitted files

- `Makefile`: `make deps` checks `dotnet-counters --version` and `dotnet-trace --version` (1.1).
- `docs/dependencies.md`: a row for each tool, after `dotnet` (1.1).
- `openspec/changes/async-performance-content/design.md`, revised after the probes:
  - D2: TODO 3's queue is now `OrderNotifications`, because CA1711 refuses `NotificationQueue`;
    TODO 4 reads results with `await` after `Task.WhenAll`, not `.Result`;
  - D3: a "Revised in §1" paragraph on the `IsCompleted`/`GetResult` pairing.
- `openspec/changes/async-performance-content/tasks.md`: 1.1–1.3 ticked; 2.2 says
  `OrderNotifications`.
- `devlog/`: `1312-diagnostics-tools`, `1330-test-probe`, `1345-page-probe`, and this note.

## Decided before the build (in the planning commit `1f98ddf`, recorded here too)

- **Emmz's calls:**
  - the extras are thread-pool diagnostics, caching, and pooling and fast APIs, and not
    concurrency primitives as a section;
  - exercise 11 fixes async bugs;
  - a benchmark project that the build gate compiles and no gate runs;
  - yes to the two diagnostics tools (1.1).
- **The lost-failure bug is a discarded task, not `async void`,** because `async void` crashes the
  test host (probe: exit 134).
- **No test asserts on elapsed time.** A side agent flagged that timing tests are flaky on CI.
  Emmz agreed to D3's IL check for blocking, with the timing test only as a fallback that needs her
  yes. 2.5 needs one CI run, not three.

## Gotchas this session

- **Getting test output from MTP and xUnit v3:**
  - `dotnet test --project X -- --report-trx` isn't available (no TRX extension), and fails with
    a handshake error;
  - `ITestOutputHelper` lines don't show in the summary.

  The probe appended its lines to a file in `AppContext.BaseDirectory` instead.
- **`pgrep` fails in the sandbox.** Use `ps -axo pid=,command=` unsandboxed, then kill by PID.
- **`dotnet-counters monitor` is interactive.** For a record, use `dotnet-counters collect --format
  csv -o file --duration hh:mm:ss`.

## Next

- **Resume 1.4:**
  1. add `<PackageVersion Include="BenchmarkDotNet" Version="0.15.8" />` to
     `samples/Directory.Packages.props`. Nothing references it until §3, so a restore check needs a
     project. Confirm with `make samples-build` and `make samples-format` (unsandboxed), and run
     `dotnet package search BenchmarkDotNet --exact-match` or the NuGet audit to check that 0.15.8
     has no advisory. **On an advisory, stop and ask;**
  2. then the §1 self-review (`git diff 1f98ddf` plus untracked files), and `make gates`. All green
     is expected: no exercise exists yet;
  3. then the section-done note with the gate exit lines, and one commit:
     `feat(async-performance-content): probe, tools and package versions (§1)`.
- **Then §2, task 2.1:** scaffold `samples/exercises/11-async`.
- **The branch has no upstream yet.** 2.5 pushes it with `git push -u origin
  change/async-performance-content` (unsandboxed, SSH).
