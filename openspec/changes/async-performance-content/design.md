# Design

## Context

- **The stub** `performance/async-and-performance.md` has a "What this covers" paragraph, two Learn
  links ("Asynchronous programming with async and await", and "Memory and spans") and the "Coming
  soon" note. It's in `EXPECTED_SLUGS` and in the Learning path, after Security & auth. Neither it
  nor a cheatsheet is in `FULL_CONTENT_SLUGS` or `CHEATSHEETS`.
- **The conventions are set** in `site/README.md` and `samples/README.md`, as for security-content:
  - the topic skeleton and the cheatsheet shape;
  - the exercise layout: `Starter`, `Solution`, and a shared `tests/` compiled into `Starter.Tests`
    and `Solution.Tests`, in both `.slnx` files;
  - central package management, xUnit v3 (ADR-0003), and warnings as errors (`AnalysisLevel`
    latest-recommended, `EnforceCodeStyleInBuild`) from `samples/Directory.Build.props`.

  Exercise 10's `tests/SecureOrdersApp.cs` (a `WebApplicationFactory<Program>` with helpers, and
  `Expect.Status` for guiding failures) is the model for 11's test app. Exercise 04 already uses
  `Microsoft.Extensions.Diagnostics.Testing`'s fake logger, at 10.11.0.
- **Decisions Emmz made** (2026-11-11): see proposal.md. In short:
  - the extras are thread-pool diagnostics, caching, and pooling and fast APIs, and not
    concurrency primitives as a section;
  - exercise 11 fixes async bugs, in memory with no Docker;
  - a BenchmarkDotNet project that the build gate compiles and no gate runs.
- **`dotnet-counters` and `dotnet-trace` aren't in `docs/dependencies.md`.** Adding them waits on
  Emmz's yes (task 1.1).
- **The site gate goes red during the samples sections.** The "Undescribed exercise is caught"
  check fails as soon as `11-async` exists, until the page links to it, as with 10 (see D10).

## Goals / Non-Goals

**Goals:**
- An engineer finishes the page able to write async code that doesn't block, honours
  cancellation, and overlaps independent work; to see starvation and allocations; and to measure a
  change before claiming it's faster.
- Every number the page states is measured, not remembered: timings, allocations, counter values,
  benchmark tables. Each says what machine it came from.
- Exercise 11 fails with messages that guide, and its tests are deterministic on a shared CI runner.

**Non-Goals:**
- Teaching the C# `async` syntax from scratch. The C# language area owns the language feature;
  this page owns its use in a service.
- Benchmarks as a gate. Their numbers vary by machine, so nothing asserts on them.
- Production tuning: GC modes, `ThreadPool` settings, server GC. One sentence each where they come
  up.

## Decisions

### D1. Page shape and order

The page follows the topic skeleton, with sections in this order:
1. **Outcome,** then the **cheatsheet tip**.
2. **Async end to end:** what `await` does with the thread while it waits; `Task` and `Task<T>`;
   async from the endpoint to the I/O; why `ConfigureAwait(false)` doesn't matter in ASP.NET Core
   code but does in libraries.
3. **Cancellation:** the endpoint's `CancellationToken` parameter (`RequestAborted`), passing it
   down, `CancellationTokenSource.CancelAfter` and linked sources for timeouts, and
   `OperationCanceledException` (and `TaskCanceledException`).
4. **Running independent work together:** `Task.WhenAll`, and the exceptions it surfaces. A
   sentence names `Parallel.ForEachAsync`, `Channel<T>`, `SemaphoreSlim` and `IAsyncEnumerable`,
   with Learn links, and no more (proposal: out of scope).
5. **Blocking, `async void` and fire-and-forget:**
   - `.Result`, `.Wait()` and `GetAwaiter().GetResult()` hold a thread;
   - `async void` crashes the process on an exception;
   - a discarded task loses its exception. Handing work to a hosted service is the fix that
     survives the request.
6. **Thread-pool starvation:** how it builds (the pool injects threads slowly), what it looks like
   (latency climbs, CPU stays low), and `dotnet-counters` and `dotnet-trace` against the same load
   on a blocking and an async endpoint (D5).
7. **`ValueTask`:** when it pays (a result that's usually ready, on a hot path), and the rules
   (await once, don't block on it).
8. **`Span<T>` and `Memory<T>`:** slicing strings and arrays without copying, `stackalloc`, and the
   rule that a span can't cross an `await` or live in a field, while `Memory<T>` can.
9. **Allocations and pooling:** why allocations cost (GC), seeing them (`[MemoryDiagnoser]`, and
   `dotnet-counters`' allocation rate), `ArrayPool<T>`, `StringBuilder` and `string.Create`,
   System.Text.Json source generation, and `FrozenDictionary` and `FrozenSet`.
10. **Caching:** `HybridCache` (stampede protection, expiry, tags and removal, and a distributed
    second level named, not set up), and output caching (`AddOutputCache`, `CacheOutput`, policies,
    and eviction by tag).
11. **Benchmarking:** a BenchmarkDotNet class, `[MemoryDiagnoser]`, running in Release, reading
    Mean, Error, StdDev and Allocated, and why a `Stopwatch` loop misleads (JIT warm-up, tiering,
    dead-code elimination). It links to the samples' benchmark project.
12. **"Coming from .NET 3.5":** a short table (D7).
13. **Pitfalls,** then **Exercise 11**.

### D2. Exercise 11 shape

**The API** (in memory; the store, the validation and the endpoints are pre-written in both
projects):
- **a pretend slow dependency** `IWarehouse`, with `ReserveAsync`, `GetStockAsync`, `GetPriceAsync`
  and `GetShippingAsync`. Each takes a `CancellationToken` and waits with `Task.Delay(…, token)`.
  The tests replace it with a controllable fake (D3);
- **`POST /orders`:** reserves stock, stores the order, then notifies the warehouse after
  answering;
- **`GET /orders/{id}/quote`:** stock, price and shipping for an order's lines, from three
  independent calls.

**The Starter "works, but badly"** (the 04 and 10 pattern). It gives correct answers, but:
1. `POST /orders` calls `ReserveAsync(…).GetAwaiter().GetResult()` in a synchronous handler
   (sync-over-async);
2. it doesn't take the request's token, so `ReserveAsync` gets `CancellationToken.None`;
3. it starts the notification as `_ = NotifyAsync(order)`, so a failure is lost;
4. `/quote` awaits the three calls one after another.

**The Starter's TODOs:**
1. Make `POST /orders` async all the way, with no blocking call.
2. Take the endpoint's `CancellationToken`, and pass it to every warehouse call.
3. Hand the notification to the pre-written `OrderNotifications` queue and its
   `BackgroundService`, which awaits each one and logs a failure at Error with the order id. The
   queue is a `Channel<T>`, pre-written so that the exercise doesn't teach channels (proposal: out
   of scope). It's not `NotificationQueue`, because CA1711 refuses a type name ending in `Queue`
   (probe note `20261111-1330`).
4. Start the three `/quote` calls together, `await Task.WhenAll`, then read each result with
   `await`, not `.Result`.

**Rejected:**
- **`async void` as the lost-failure bug.** A thrown exception in an `async void` method crashes the
  process, so the Starter would kill the test host instead of failing one test. The page covers it
  instead (D1, section 5).
- **A real database.** It adds Docker and nothing async that `Task.Delay` doesn't show.

### D3. Detecting each bug deterministically

**No test depends on how fast the runner is.** Each one either waits for an event (with a timeout
that only bounds how long a failing run takes to report), or inspects the code. None of them
asserts on elapsed time.
- **Sequential awaits:** the fake warehouse's three quote calls each wait on a shared barrier that
  releases only when all three are in flight, with a 5-second timeout. Concurrent calls pass at
  once. Sequential calls time out, and the test fails with "the quote's calls ran one after another
  (TODO 4)". No timing threshold.
- **Cancellation:** the fake's `ReserveAsync` waits on its token, and records whether it was
  cancelled. The test sends `POST /orders` with a client token that it cancels once the fake reports
  that it started. Then it asserts that the fake saw the cancellation. **The probe (1.2) confirms**
  that `TestServer` turns a cancelled client request into `RequestAborted`.
- **Lost failure:** the fake's notification throws. The test waits up to 5 seconds for an Error log
  entry with the order id, through the fake logger collector. It fails with a TODO 3 hint if none
  arrives, and checks that the app still answers.
- **Blocking: inspect the handler, don't time it.** At run time, only timing tells a blocked thread
  from a waiting one, and timing tests fail at random on a busy shared runner. So the test checks the
  code instead:
  - it finds the `POST /orders` endpoint in the test app's `EndpointDataSource`, and takes the
    handler's `MethodInfo` from the endpoint's metadata (minimal APIs put it there);
  - **the handler must return `Task` or `Task<T>`.** That catches a synchronous handler;
  - **the handler's IL must call none of `Task.Result`, `Task<T>.Result`, `Task.Wait`,
    `TaskAwaiter.GetResult` or `TaskAwaiter<T>.GetResult`.** That catches a blocking call inside an
    `async` handler. For an `async` lambda the body is in the compiler's state machine, so the test
    follows `AsyncStateMachineAttribute` to its `MoveNext`, and reads the IL there. It resolves each
    `call`/`callvirt` token with `Module.ResolveMethod`. A small IL reader in `tests/` does this,
    about 40 lines, with no package;
  - the failure names the call it found ("POST /orders calls Task<T>.Result: it holds a thread while
    the warehouse works. Await it instead (TODO 1)").

  It's deterministic, and runs in the normal collection. **The probe (1.2) runs it against four
  handlers:** synchronous with `.GetAwaiter().GetResult()`, `async` with `.Result` inside, `async`
  with `.Wait()`, and correct. The first three must fail and the last must pass. **The limit:** it
  checks the handler and its state machine only, not the methods they call. The Starter's blocking
  call is in the handler, and TODO 1 says to keep the work there. A helper that blocks is a page
  pitfall, not a test case.

  **Revised in §1** (probe note `20261111-1330`): the compiler's own `await` calls the awaiter's
  `GetResult()` in `MoveNext`, after an `IsCompleted` check. So the check flags a `GetResult()`
  only when there's no `IsCompleted` check to pair it with. `Task.Result` and `Task.Wait` are never
  compiler-emitted, so any call to them counts. All six probe handlers came out as expected.

  **Fallback, only with Emmz's agreement:** if the probe finds the IL check unworkable, a timing
  test (twice as many concurrent requests as the pool's minimum threads, against a 1-second fake,
  all done within 3 seconds, in its own collection with parallelism off). It would need Emmz's yes,
  because it can fail at random.

### D4. The benchmark project

- **`samples/benchmarks/Benchmarks`** (one project, `Benchmarks.csproj`), in `Primer.slnx` only.
  `make samples-build` compiles it. `make samples-test` uses `Primer.Solutions.slnx`, so it never
  runs it (spec: "Benchmark project builds but isn't run by the gates").
- **Benchmarks,** each a class with a baseline and `[MemoryDiagnoser]`, one per page example:
  - parsing an order line: `string.Split` against `ReadOnlySpan<char>` slicing;
  - building a string: `+=` in a loop, `StringBuilder`, and `string.Create`;
  - a buffer: `new byte[]` against `ArrayPool<byte>.Shared`;
  - JSON: reflection-based `JsonSerializer` against a source-generated `JsonSerializerContext`;
  - lookups: `Dictionary` against `FrozenDictionary`.
- **`Program.cs`** runs `BenchmarkSwitcher.FromAssembly(…).Run(args)`, so `--filter` picks a
  class.
- **The page's tables come from running it** on this Mac (`dotnet run -c Release --project
  samples/benchmarks/Benchmarks -- --filter '*'`). The page states the machine, the OS and the SDK
  as BenchmarkDotNet prints them. The numbers will differ elsewhere; the ratios are the point.
- **Analyzer friction:** BenchmarkDotNet needs public, non-sealed classes and instance methods,
  which can trip `latest-recommended` (CA1822, CA1515 and others). The project relaxes only the
  rules it must, in its `.csproj` or a local `.editorconfig`, and section 3's note says which.

### D5. Diagnostics with `dotnet-counters` and `dotnet-trace`

- **The scratch app** has `/blocking` (sync-over-async on a 500 ms delay) and `/async` (the same,
  awaited). A small `node` script puts the same concurrent load on each.
- **`dotnet-counters monitor -n <app> --counters System.Runtime`** during each run: the page shows
  the thread-pool thread count and queue length, using the counter names .NET 10 prints. The probe
  records the names.
- **`dotnet-trace collect`** during the blocking run, then a view that shows the blocking frame:
  the probe tries `--format speedscope` and `dotnet-trace report … topN`, and the page shows
  whichever names the blocking call with no extra tool. If neither does, the page shows the
  command and says to open the trace in Visual Studio or PerfView, and the probe note says so.
- **Versions:** the global tools at their current release, installed with
  `dotnet tool install -g`. Their rows in `docs/dependencies.md`, and their `make deps` checks, need
  Emmz's yes (1.1).

### D6. Caching

- **`HybridCache`:** `Microsoft.Extensions.Caching.Hybrid`, at the version the probe finds. The
  page's example counts factory calls, and shows 1 call for 50 concurrent requests for one key. It
  also shows `RemoveAsync`, and `RemoveByTagAsync` if the probe confirms that it works with the
  in-memory level only. The package is compiled only in the scratch project, unless the exercise
  needs it (it doesn't).
- **Output caching:** `AddOutputCache` and `UseOutputCache`, `.CacheOutput(…)` with an expiry and a
  tag, and `IOutputCacheStore.EvictByTagAsync`. The page shows how to tell that a response was
  cached: the `Age` header, if the probe sees it, or a counter in the handler that doesn't move.
- **The pitfall:** output caching skips authenticated requests and responses that set cookies by
  default. The probe confirms this before the page states it.

### D7. The "Coming from .NET 3.5" table

Rows, each a .NET 3.5 habit and what to do now:
- **`BeginXxx`/`EndXxx` and `IAsyncResult`** → `async` methods and `await`;
- **the event-based pattern** (`XxxAsync` plus `XxxCompleted`, `BackgroundWorker`) → `await`, with
  `IProgress<T>` for progress;
- **`ThreadPool.QueueUserWorkItem`** → `Task.Run` for CPU work only, and never in a request to
  make sync code "async";
- **`new Thread(…)` for background work** → a `BackgroundService`;
- **`HttpRuntime.Cache` and `Cache.Insert`** → `HybridCache`, or output caching;
- **string concatenation in a loop** → `StringBuilder`, or `string.Create` when the length is known;
- **`Thread.Sleep` to wait** → `await Task.Delay(…, token)`;
- **timing with `DateTime.Now` and loops** → BenchmarkDotNet.

### D8. Cheatsheet

It follows the cheatsheet shape (a back link, `##` groups of copyable blocks, minimal prose):
- async endpoints and passing the token, timeouts, and `Task.WhenAll`;
- blocking calls to avoid, and their replacements, as a table;
- a hosted background worker for fire-and-forget work;
- `dotnet-counters` and `dotnet-trace` commands;
- `ValueTask` rules;
- `Span<T>`, `Memory<T>` and `stackalloc`;
- `ArrayPool<T>`, `string.Create`, a source-generated `JsonSerializerContext`, and frozen
  collections;
- `HybridCache` and output caching;
- a BenchmarkDotNet skeleton, and the run command.

### D9. Packages

**`PackageVersion` entries** for `samples/Directory.Packages.props`:
- **`BenchmarkDotNet`,** at the current release (1.3 records it), for the benchmark project only;
- **`Microsoft.Extensions.Diagnostics.Testing` 10.11.0** is already there, for 11's log check.

`Microsoft.Extensions.Caching.Hybrid` is compiled only in the scratch project. Exercise 11 needs
no new package: `Channel<T>` and `BackgroundService` are in the shared framework.

### D10. Section order

1. Probe, tools and package versions (D3, D5, D6, D9).
2. Exercise 11: scaffold, Solution, tests, Starter, CI.
3. The benchmark project.
4. The cheatsheet, then the Async & performance page.
5. The samples README.
6. The human check.

As before, the exercise and the benchmarks come before the page, so the page's "from the
exercise" snippets and benchmark tables are real. **§2 and §3 land with `SITE_TEST_EXIT:1` on
purpose:** that's the undescribed-exercise check for `11-async`. §4 turns it green. The section
notes and commit bodies record it.

### D11. Code accuracy and verification

As in earlier changes:
- every snippet is compiled in a scratch project, net10.0, with warnings as errors;
- "from the exercise" snippets are copies of the real files;
- every URL on both pages is fetched.

Also, by running them:
- **cancellation:** a client that disconnects mid-request, with the exception the server sees;
- **`Task.WhenAll`:** two independent 500 ms awaits, timed together and one after another;
- **`async void`:** the crash, with its real message, in a scratch console app;
- **starvation:** the `dotnet-counters` values for both endpoints under the same load (D5);
- **span parsing, pooling, strings, JSON and frozen collections:** the benchmark project's tables;
- **`HybridCache`:** the factory call count under concurrent requests;
- **output caching:** the second response is cached, and an authenticated request isn't.

## Risks / Trade-offs

- **The blocking test reads IL, so it's tied to how the compiler lowers lambdas.** A future
  compiler could move an `async` lambda's body somewhere the test doesn't look. → The test fails
  loudly if it can't find the handler's `MoveNext` ("couldn't find the handler's body to check"),
  rather than passing silently. The Solution's passing run proves it finds the body today.
- **The IL check sees only the handler.** A blocking call in a method the handler calls gets past
  it. → The Starter's bug is in the handler, and the page's Pitfalls cover blocking in helpers.
- **Benchmark numbers date, and vary by machine.** → The page states the machine and SDK, talks
  in ratios, and the project lets anyone rerun it.
- **Counter names change between releases.** .NET 9 renamed runtime counters to the
  `dotnet.thread_pool.*` meter names. → The probe records the names .NET 10 prints, and the page
  quotes them.
- **`dotnet-trace` output needs a viewer.** → D5's fallback: show the command and name the viewers.
- **`HybridCache` API details moved between previews and GA.** → Compiled and run against the
  current package in the probe; nothing from memory.
