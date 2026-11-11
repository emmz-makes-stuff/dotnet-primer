# Proposal

## Why

Async & performance is the next placeholder on the learning path, after Security & auth. Emmz chose
it as the next change on 2026-11-11. The engineers come from .NET 3.5, which had no `async`/`await`:
asynchronous code meant `BeginXxx`/`EndXxx` pairs, the event-based pattern and `ThreadPool`
callbacks, and performance work meant arrays, `StringBuilder` and hope. An ASP.NET Core service on
.NET 10 is async end to end. The commonest production faults in code ported from that world are
async ones: blocking on tasks, ignored cancellation, `async void`, and work done one at a time that
could overlap. The page also gives the tools to find and measure them.

## What Changes

- **Write the full Async & performance page** (`performance/async-and-performance`), replacing the
  stub. It follows the stub's outline, plus the three extras Emmz picked on 2026-11-11 (thread-pool
  diagnostics, caching, and pooling and fast APIs; not concurrency primitives as a section of their
  own):
  - **Async end to end:** what `await` does, `Task` and `Task<T>`, async all the way down from the
    endpoint, and `ConfigureAwait` in ASP.NET Core.
  - **Cancellation:** `CancellationToken` from the request (`HttpContext.RequestAborted`), passing it
    down, timeouts with `CancelAfter`, and `OperationCanceledException`.
  - **Sync-over-async and `async void`:** why `.Result`, `.Wait()` and `GetAwaiter().GetResult()`
    block a thread, and what `async void` does to an exception.
  - **Running work concurrently:** `Task.WhenAll` for independent awaits. This is a subsection of
    async, not a concurrency-primitives section.
  - **`ValueTask`:** when it helps, and the rule of awaiting it once.
  - **Thread-pool starvation:** how blocking calls starve the pool, and diagnosing it with
    `dotnet-counters` and `dotnet-trace`.
  - **`Span<T>` and `Memory<T>`:** slicing without copying, `stackalloc`, and where each can live.
  - **Allocations and pooling:** reading allocations, `ArrayPool<T>`, `StringBuilder` and
    `string.Create`, System.Text.Json source generation, and frozen collections.
  - **Caching:** `HybridCache` and output caching.
  - **Benchmarking:** BenchmarkDotNet, with `[MemoryDiagnoser]`, and reading its results.
  - **Coming from .NET 3.5:** a short table, after the concepts and before the Pitfalls.
  - **Pitfalls,** then **Exercise 11**.
- **An Async & performance cheatsheet** (`cheatsheets/async-and-performance`). It is linked both
  ways with the page, listed in the sidebar and on the introduction, and added to the site tests'
  maps.
- **Exercise 11** (`samples/exercises/11-async`): a starter, a solution and shared tests, like
  01–08 and 10. It is **standalone, with an in-memory order store and a pretend slow dependency, so it
  needs no Docker**. The engineer fixes an orders API that works, but badly:
  - sync-over-async that blocks a thread;
  - a cancellation token that isn't passed down;
  - a fire-and-forget call whose failure is silently lost (not `async void`: that crashes the
    process, test host and all, so the page covers it and the exercise doesn't);
  - independent awaits run one after another.

  **The shared tests catch each bug** with a message that names its TODO.
- **A benchmark project** (`samples/benchmarks`), with BenchmarkDotNet benchmarks for the page's
  pooling and fast-API examples. The engineer runs it by hand. **The samples build gate compiles it,
  but no gate runs it** (Emmz, 2026-11-11), because benchmarks are slow and noisy on shared runners.
- **The samples README:** the layout and the exercise table gain 11, and the benchmark project and
  how to run it.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- **`topic-page-content`:**
  - "Full-content pages carry no placeholder" adds the Async & performance page and its cheatsheet.
  - New: "Async page covers async end to end and cancellation", "… covers blocking and thread-pool
    starvation", "… covers spans, allocations and pooling", "… covers caching", "… covers
    benchmarking", and "Async page maps .NET 3.5 habits".
- **`site-structure`:** "Cheatsheets per area" grows from eleven cheatsheets to twelve.
- **`sample-exercises`:**
  - "Site describes each exercise" gains a scenario for exercise 11.
  - New: "Async exercise fixes blocking and cancellation", "Async exercise fixes lost failures and
    sequential awaits", and "Benchmark project builds but isn't run by the gates".

## Impact

- **Site:**
  - `site/src/content/docs/performance/async-and-performance.md`, and a new
    `site/src/content/docs/cheatsheets/async-and-performance.md`;
  - the sidebar's Cheatsheets group in `site/astro.config.mjs`;
  - the cheatsheet cards on `site/src/content/docs/index.mdx`;
  - `site/tests/dist.test.ts` (`EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS`, `CHEATSHEETS`).
- **Samples:**
  - `samples/exercises/11-async`, `samples/benchmarks`, and both `.slnx` files (the benchmark
    project goes in `Primer.slnx` only, so the test gate never runs it);
  - new `PackageVersion` entries in `samples/Directory.Packages.props`: `BenchmarkDotNet`, and
    `Microsoft.Extensions.Caching.Hybrid` if the probe finds the page's snippets need it in the
    samples (otherwise it's compiled only in the scratch project);
  - `samples/README.md`.
- **Tooling: needs Emmz's agreement.** `dotnet-counters` and `dotnet-trace` are .NET global tools,
  and neither is in `docs/dependencies.md`. The page's diagnostics section shows their real output,
  so the probe needs them. With Emmz's yes, the section that first uses them adds their rows to
  `docs/dependencies.md` and their checks to `make deps`, and says why in a devlog note. The
  engineers only need them to follow that section, not for exercise 11.
- **Out of scope:**
  - concurrency primitives as a section of their own: `Channel<T>`, `Parallel.ForEachAsync`,
    `SemaphoreSlim` throttling and `IAsyncEnumerable` (Emmz, 2026-11-11), beyond a sentence that
    names them;
  - distributed caching setup (Redis), beyond naming it as `HybridCache`'s second level;
  - profilers other than `dotnet-counters` and `dotnet-trace` (PerfView, Visual Studio, dotMemory);
  - Native AOT and trimming;
  - running benchmarks in CI.
