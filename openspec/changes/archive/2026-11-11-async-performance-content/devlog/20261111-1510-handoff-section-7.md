# Handoff: §7 built (7.1–7.6 ticked), uncommitted; verify, gate and commit next

<!-- Section: 7 · Tasks: 7.1–7.6 ticked, not committed · Base: afc582a -->

The context passed 40% after 7.6. **Nothing in §7 is committed.** The section commits once, after
the checks below and the gates.

## Done this session (all uncommitted)

- **7.1** `OrderNotifications.cs` (Solution and Starter, the same file):
  - `Add` throws `InvalidOperationException` once the queue is completed, rather than dropping the
    order;
  - `Complete()` is new, and `ReadAllAsync()` takes no token;
  - `NotificationWorker` reads with no token, and sends with a private `_outOfTime` CTS, which
    `StopAsync` cancels only when the host's shutdown token fires. `StopAsync` calls `Complete()`
    first. A new `LogNotSent(orderId)` logs at Error each order it had no time for.
    `Dispose` disposes the CTS.
  - **Tests**, deterministic, calling the worker's own `StopAsync` (found by
    `GetServices<IHostedService>().OfType<NotificationWorker>()`):
    - `QueuedNotificationsAreSentWhenTheAppStops`: hold notifications, place 3, stop, then release.
      It expects all 3 sent. It passes on the Starter too (its `_ =` calls finish once released),
      which is fine;
    - `UnsentNotificationsAreLoggedWhenStoppingRunsOutOfTime`: `StopAsync(new
      CancellationToken(true))`, and expects an Error with each id. It fails on the Starter, with
      the TODO 3 hint.
  - New in the fake: `HoldNotifications`, `NotifyStarted`, `ReleaseNotifications()` and `Notified`.
  - New helper in the tests: `ErrorLoggedFor(app, id)`.
  - **Page** ("Blocking, async void and fire-and-forget"): the `From exercise 11:
    OrderNotifications.cs` block now shows the new `ExecuteAsync` and `StopAsync`, with a paragraph
    on draining when the app stops (30-second default shutdown timeout) and durable queues (SQS).
    **Cheatsheet** "Background work": the same pattern, compact, with a line on durable queues.
- **7.2** `BlockingCalls` matches `WaitAll` and `WaitAny` too, and its doc comment says so.
- **7.3** The fake's quote rendezvous is thread-free: an `Interlocked` counter and a
  `TaskCompletionSource`, with `WaitAsync(QuoteTimeout, token)`. The `Barrier` is gone.
- **7.4** Page, Benchmarking:
  - BenchmarkDotNet handles warm-up, tiering and dead code, and **refuses a Debug build**. Checked:
    a Debug run exits 0, runs nothing, and prints `Assembly Benchmarks which defines benchmarks is
    non-optimized` (that's the text the page quotes);
  - Error: "very probably within Mean ± Error, though not certainly", with BDN's normality warning.
- **7.5** **The finding checked out, but its first suggested fix was wrong.** Allocations for one
  join (a file-based scratch check, run in Release):
  - `result += "," + sku`: 75 192 B;
  - `string.Concat(result, ",", sku)`: 71 232 B;
  - two `+=` appends: 141 304 B, which doubles the copies.

  So `Concatenate` is now `result = i == 0 ? _skus[i] : result + "," + _skus[i];`, which compiles
  to one Concat, with a comment saying why. The re-run (`--filter '*StringBuilding*'`, Release,
  the same environment block, no warnings) gave:

  | Method | Mean | Error | StdDev | Ratio | Gen0 | Gen1 | Allocated | Alloc Ratio |
  | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
  | Concatenate | 3,243.2 ns | 15.25 ns | 12.74 ns | 1.00 | 8.5144 | 0.0229 | 69.56 KB | 1.00 |
  | Builder | 439.3 ns | 2.80 ns | 2.48 ns | 0.14 | 0.4730 | 0.0043 | 3.87 KB | 0.06 |
  | Create | 273.8 ns | 2.30 ns | 2.15 ns | 0.08 | 0.1678 | - | 1.38 KB | 0.02 |

  The page's table and its "Building strings" sentence are updated (now `result = result + "," +
  sku`). No other page, cheatsheet or README quotes the old numbers (grep).
- **7.6** `make deps` checks `dotnet-counters`/`dotnet-trace --version | grep -q "^10\."`. Both
  print `10.0.750501+…`, and `DEPS_EXIT:0`.

## Verified so far

- Solution 7/7, three runs in a row (about 1.1 s each). Starter 2/7: the baseline and
  `QueuedNotifications…` pass.
- Leave-one-out (`scratchpad/leave-one-out.sh`, gone after a clear):
  - `all` 7/7;
  - `no1`, `no2` and `no4` fail only their own test. `no3` fails `FailedNotificationIsLogged` and
    `UnsentNotifications…`, both TODO 3's;
  - `waitall` fails the blocking test ("calls Task.WaitAll"), and also the cancellation test.
    That's real: `WaitAll` wraps the cancel in `AggregateException`, which `TestServer` passes to
    the client;
  - `waitany` (written as `Task.WaitAny([…], cancellationToken)`, because CA2016 refuses it
    without the token) fails only the blocking test;
  - `oldworker` (the `afc582a` worker) fails both shutdown tests.

## Still to do for §7

1. **Re-check the page and cheatsheet code**, as 4.3 did: the changed page block and the new
   cheatsheet block must compile, with warnings as errors, against the real exercise files. The
   page's `OrderNotifications.cs` block must match the file line for line. The cheatsheet block
   uses `using var _ = …` and calls `ThrowIfCancellationRequested` inside the `try`, so watch
   those. The 4.3 harness (`scratchpad/snippets/`) is gone, so rebuild it as `1440` describes:
   a wrapper per block, the samples' build props, and two passes for the `OrderJsonContext`
   clash.
2. Self-review `git diff afc582a`, then `make gates` (unsandboxed), looking for `GATES_EXIT:0`.
3. Run `make samples-format`, because IDE1006 bit in §2.
4. A section-done note with the exit lines, then one commit:
   `feat(async-performance-content): review fixes (§7)`, with 7.1–7.6 in the body.
5. Push. `afc582a` (change-done) is unpushed too.

## Gotchas

- `grep 'Async.Starter'` matches `Async.Starter.Tests` too. Match the binary path before killing
  anything, and check for orphaned test hosts after a hang.

## Next

- **Finish §7:** steps 1–5 above.
- **Then the finishing steps:** a new change-done note. Code-review has already run: don't rerun it
  unless Emmz asks. Then the merge to `main` with her go-ahead, the live-site check (both tree
  links), and `/opsx:archive`, after `openspec validate --specs --strict`.
- Panopticon: `mcp__plugin_panopticon_panopticon__report_progress` (panopticon.team).
