---
title: "Async & performance"
description: "async and await end to end, cancellation and timeouts, blocking and thread-pool starvation, dotnet-counters and dotnet-trace, ValueTask, Span<T> and Memory<T>, allocations and pooling, HybridCache and output caching, BenchmarkDotNet, and coming from .NET 3.5."
learn:
  - title: "Asynchronous programming with async and await"
    url: https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/
  - title: "ASP.NET Core best practices"
    url: https://learn.microsoft.com/aspnet/core/fundamentals/best-practices
  - title: "Cancellation in managed threads"
    url: https://learn.microsoft.com/dotnet/standard/threading/cancellation-in-managed-threads
  - title: "Debug ThreadPool starvation"
    url: https://learn.microsoft.com/dotnet/core/diagnostics/debug-threadpool-starvation
  - title: "Memory- and span-related types"
    url: https://learn.microsoft.com/dotnet/standard/memory-and-spans/
  - title: "How to use source generation in System.Text.Json"
    url: https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation
  - title: "HybridCache library in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/performance/caching/hybrid
  - title: "Output caching middleware in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/performance/caching/output
---

By the end of this page you'll be able to keep an ASP.NET Core app's threads free while it waits: async from the endpoint down to the I/O, cancelled when the caller goes, with independent calls run together. You'll be able to spot a blocked thread pool with `dotnet-counters` and find the blocking call with `dotnet-trace`, cut allocations with spans and pools, cache data and whole responses, and prove a speed-up with BenchmarkDotNet rather than a `Stopwatch`.

:::tip[Cheatsheet]
Keep the [Async & performance cheatsheet](/cheatsheets/async-and-performance/) open while you work.
:::

The async examples are the orders API from [exercise 11](#exercise-11-unblock-the-orders-api). It calls a slow warehouse to reserve stock, to quote prices, and to hear about new orders. The performance numbers come from the samples' [benchmark project](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/benchmarks/Benchmarks), run on this machine:

```text
BenchmarkDotNet v0.15.8, macOS Tahoe 26.6.2 (25G83) [Darwin 25.6.0]
Apple M3 Max, 1 CPU, 14 logical and 14 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), Arm64 RyuJIT armv8.0-a
```

Your numbers will differ. The ratios are the point.

## Async end to end

A web server has a pool of threads, and each request runs on one of them. When a request waits for a database, a file or another service, there are two ways to wait:

- **Blocking:** the thread sits idle until the answer comes back. Nothing else can use it.
- **`await`:** the method returns its unfinished `Task` to the caller, and the thread goes back to the pool to run other requests. When the answer arrives, the rest of the method runs, on whichever pool thread is free.

So `await` doesn't make a single request faster. It lets a handful of threads serve thousands of requests that are mostly waiting.

A method marked `async` returns `Task` (no result) or `Task<T>` (a result of type `T`). `await` unwraps the result, or rethrows the exception the task failed with:

```csharp title="From exercise 11: Warehouse.cs"
public async Task<long> GetPriceAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken)
{
    await Task.Delay(_latency, cancellationToken);
    return lines.Sum(l => _pricesCents.GetValueOrDefault(l.Sku) * l.Quantity);
}
```

The gain only comes if the code is async all the way: from the endpoint, through your services, to the call that does the I/O. One blocking call anywhere in that chain holds the thread, and the rest of the chain's `await`s don't help ([blocking](#blocking-async-void-and-fire-and-forget)).

:::note[ConfigureAwait(false)]
ASP.NET Core has no synchronization context, so `ConfigureAwait(false)` changes nothing in app code. Leave it out. In a library that UI apps might call, keep it on every `await`, so that the library doesn't need the UI thread to carry on.
:::

## Cancellation

When a caller gives up (a closed browser tab, a client timeout), the work it started is wasted. ASP.NET Core signals it through `HttpContext.RequestAborted`, and a minimal API binds a `CancellationToken` parameter to that token. Take it, and pass it to every call that waits:

```csharp title="From exercise 11: Program.cs"
app.MapPost("/orders", async (PlaceOrderRequest request, OrderStore store, IWarehouse warehouse,
    OrderNotifications notifications, CancellationToken cancellationToken) =>
{
    var problems = request.Problems();
    if (problems.Count > 0)
    {
        return Results.ValidationProblem(problems);
    }

    await warehouse.ReserveAsync(request.Lines!, cancellationToken);
    var order = store.Add(request.Lines!);

    // TODO 3 (solved): the notification goes to the queue, and NotificationWorker awaits it, so a
    // failure is logged. The request doesn't wait for it, and its token doesn't cancel it.
    notifications.Add(order);

    return Results.Created($"/orders/{order.Id}", order);
});
```

A cancelled call throws `OperationCanceledException`, or its subclass `TaskCanceledException`. When a client aborted a request after 300 ms, against an endpoint awaiting `Task.Delay(10_000, cancellationToken)`, the server saw:

```text
System.Threading.Tasks.TaskCanceledException: A task was canceled.
```

ASP.NET Core doesn't treat that as an error in your code: the client has gone, so there's no one to send a 500 to. Catch it only to clean up, and then rethrow. Catch it with `when (cancellationToken.IsCancellationRequested)`, so that you don't swallow a timeout you didn't cause.

**Timeouts** are cancellation on a timer. `CancelAfter` gives one call a deadline, and a linked source keeps the caller's token too:

```csharp
using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
deadline.CancelAfter(TimeSpan.FromSeconds(2));
await warehouse.ReserveAsync(lines, deadline.Token);    // cancelled by the caller, or after 2 s
```

For a whole request, `AddRequestTimeouts()`, `UseRequestTimeouts()` and `.WithRequestTimeout(…)` on the endpoint cancel `RequestAborted` after the time you set. They don't apply while a debugger is attached.

## Running independent work together

`await` one call, then the next, and each waits for the one before. Start them all first, then wait for them together with `Task.WhenAll`:

```csharp title="From exercise 11: Program.cs"
var stock = warehouse.GetStockAsync(order.Lines, cancellationToken);
var price = warehouse.GetPriceAsync(order.Lines, cancellationToken);
var shipping = warehouse.GetShippingAsync(order.Lines, cancellationToken);
await Task.WhenAll(stock, price, shipping);

// They're all done, so these awaits don't wait. They read the results without .Result.
return Results.Ok(new Quote(order.Id, await stock, await price, await shipping));
```

Two 500 ms awaits took **1002 ms** one after another, and **501 ms** with `Task.WhenAll`: the time of the slower one, not the sum.

If any task fails, `await Task.WhenAll(…)` throws the first exception, once every task has finished. The others are on the returned task's `Exception.InnerExceptions`. Only do this for calls that really don't depend on each other.

For more than a handful of calls, look at [`Parallel.ForEachAsync`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.parallel.foreachasync) (with a limit on how many run at once), [`Channel<T>`](https://learn.microsoft.com/dotnet/core/extensions/channels) for producers and consumers, [`SemaphoreSlim`](https://learn.microsoft.com/dotnet/api/system.threading.semaphoreslim) to cap concurrency by hand, and [`IAsyncEnumerable<T>`](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream) to stream results.

## Blocking, async void and fire-and-forget

**Blocking on a task** (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) gives you the answer, but holds the thread until it comes. This is "sync over async", and it's the most common way an ASP.NET Core app runs out of threads. Exercise 11's starter does it:

```csharp title="From exercise 11: Starter/Program.cs"
warehouse.ReserveAsync(request.Lines!, CancellationToken.None).GetAwaiter().GetResult();
```

The fix is to make the method `async` and `await` it, and so up the chain to the endpoint.

**`async void`** is for event handlers only. Nothing can await it, so an exception thrown inside it has nowhere to go: the runtime raises it on the thread pool, and **the process ends**. A console app calling `async void SendReceipt(42)`, which throws after an `await`, printed:

```text
Receipt sent, carrying on.
Unhandled exception. System.InvalidOperationException: The mail server refused the receipt for order 42.
```

Then the stack trace, and exit code 134. "Still running." never printed. In a web app, that's every request in flight gone. Write `async Task` instead, and await it.

**A discarded task** (`_ = NotifyAsync(order)`) doesn't crash anything, but if it fails, nobody ever knows. And if it needs the request's services, they may be disposed before it finishes. Work that must outlive the request belongs to a hosted service, which awaits it and logs a failure:

```csharp title="From exercise 11: OrderNotifications.cs"
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await foreach (var order in notifications.ReadAllAsync())
    {
        if (_outOfTime.IsCancellationRequested)
        {
            LogNotSent(order.Id);
            continue;
        }

        try
        {
            await warehouse.NotifyAsync(order, _outOfTime.Token);
        }
        catch (OperationCanceledException) when (_outOfTime.IsCancellationRequested)
        {
            LogNotSent(order.Id);
        }
        catch (Exception exception)
        {
            // Any failure is logged, so the worker carries on with the next order.
            LogNotifyFailed(exception, order.Id);
        }
    }
}

public override async Task StopAsync(CancellationToken cancellationToken)
{
    // No more orders: ExecuteAsync sends what's queued, then ends.
    notifications.Complete();

    // cancellationToken is cancelled when the host's shutdown timeout runs out.
    using var outOfTime = cancellationToken.Register(_outOfTime.Cancel);
    await base.StopAsync(cancellationToken);
}
```

The request adds the order to `OrderNotifications` (a `Channel<T>`) and answers at once. The worker is registered with `builder.Services.AddHostedService<NotificationWorker>()`.

**Stopping is where background work gets lost.** When the app stops, the host cancels `stoppingToken` straight away. A worker that reads its queue with that token, or passes it on, drops every order still queued, without a word. So this one doesn't. `StopAsync` closes the queue, the worker sends what's left, and it only gives up when the host's shutdown timeout (30 seconds by default) runs out. Then it logs every order it never sent. An in-memory queue still can't survive a crash or a killed container, though: when losing the work isn't acceptable, put it on a durable queue, such as Amazon SQS, and let a worker take it from there.

## Thread-pool starvation

The pool makes threads freely up to its minimum, one per CPU core. Past that, when every thread is busy and work is queued, it adds them slowly, so that a short burst doesn't create hundreds: on this 14-core machine, it had reached 29 threads after 10 seconds of starvation. With blocking code, every request needs its own thread for its whole wait, so the queue grows faster than the pool does. Latency climbs, CPU stays low, and the app looks hung, although it's only waiting.

The same 200 concurrent requests, each waiting 500 ms on this machine, against `Task.Delay(500).Wait()` and against `await Task.Delay(500)`:

| Endpoint    | Total     | p50       | p99       | `dotnet.thread_pool.thread.count` max | `dotnet.thread_pool.queue.length` max |
| ----------- | --------- | --------- | --------- | ------------------------------------- | ------------------------------------- |
| `/blocking` | 28 145 ms | 27 885 ms | 27 906 ms | 29                                    | 168                                   |
| `/async`    | 605 ms    | 577 ms    | 583 ms    | 12                                    | 0                                     |

The blocking endpoint took 28 s to serve what the async one served in 0.6 s, and the pool had grown to only 29 threads. (The blocking maxima are from the first 10 s of the run.)

### See it with dotnet-counters

```bash
dotnet tool install -g dotnet-counters
dotnet-counters ps
dotnet-counters monitor -n Async.Solution --counters System.Runtime
```

Watch `dotnet.thread_pool.thread.count` and `dotnet.thread_pool.queue.length`. A thread count that keeps climbing while the queue length stays above zero is starvation. To keep a record instead, `dotnet-counters collect` writes the same counters to a file (`--format csv -o counters.csv`).

### Find the blocking call with dotnet-trace

```bash
dotnet tool install -g dotnet-trace
dotnet-trace collect -p <pid> --duration 00:00:00:06 -o blocking.nettrace
dotnet-trace report blocking.nettrace topN -n 25 --inclusive
```

During a 100-request blocking load, the inclusive view showed:

```text
5.  ManualResetEventSlim.Wait(int32,value class System.Threading.Cancellat    54.15%    28.29%
6.  Task.InternalWait(int32,value class System.Threading.CancellationToken    53.16%    0%
...
9.  EndpointMiddleware.Invoke(class Microsoft.AspNetCore.Http.HttpContext)    52.18%    0%
10. Anonymously Hosted DynamicMethods Assembly!dynamicClass.lambda_method1    52.18%    0%
```

Read it as "half the time, a request is in `Task.Wait`, under the endpoint". Use `--inclusive`: the default, exclusive view leads with `LowLevelLifoSemaphore.WaitNative`, which is idle pool threads waiting for work, not the problem.

## ValueTask

Every `async` method that returns `Task<T>` allocates a `Task` object, even when the answer is ready at once. `ValueTask<T>` avoids that when the result is usually ready already, such as a cache hit on a hot path:

```csharp
public ValueTask<long> GetPriceAsync(string sku, CancellationToken cancellationToken) =>
    _prices.TryGetValue(sku, out var price)
        ? ValueTask.FromResult(price)                                  // no Task allocated
        : new ValueTask<long>(LoadPriceAsync(sku, cancellationToken));
```

The rules: await a `ValueTask` **once, and only once**. Don't read `.Result` before it has completed, and don't pass it to `Task.WhenAll` without `.AsTask()`. A `ValueTask` can be backed by a pooled object that's reused once it has been awaited. Return `Task` unless a measurement says the allocation matters.

## Span\<T> and Memory\<T>

A `Span<T>` is a view of a run of memory: part of an array, a string, or the stack. Slicing it copies nothing. Parsing an order line like `SKU-1,2,1250` with `string.Split` makes an array and three strings; slicing a `ReadOnlySpan<char>` reads the same characters where they are:

```csharp title="From the benchmark project: OrderLineParsing.cs"
var line = _line.AsSpan();
var first = line.IndexOf(',');
var sku = line[..first];
var rest = line[(first + 1)..];
var second = rest.IndexOf(',');
if (!sku.StartsWith("SKU-", StringComparison.Ordinal))
{
    return -1;
}

return int.Parse(rest[..second], CultureInfo.InvariantCulture) * long.Parse(rest[(second + 1)..], CultureInfo.InvariantCulture);
```

| Method |     Mean |    Error |   StdDev | Ratio | Allocated |
| ------ | -------: | -------: | -------: | ----: | --------: |
| Split  | 34.29 ns | 0.487 ns | 0.432 ns |  1.00 |     136 B |
| Span   | 10.24 ns | 0.314 ns | 0.897 ns |  0.30 |         - |

About three times faster, with nothing allocated. (BenchmarkDotNet flagged the span timings as having more than one mode, so take the 10.24 ns as "about 10".)

`stackalloc` gives a span on the thread's stack, with no garbage to collect. Keep it small and fixed: `Span<byte> scratch = stackalloc byte[256];`.

**Where a span can't go:** a span lives on the stack, so it can't be a field of a class, be boxed, or be in use across an `await` (the method's state moves to the heap there). When you need to keep a buffer, or use it across an `await`, use `Memory<T>`, and take `.Span` from it between awaits:

```csharp
Memory<byte> buffer = new byte[4096];
var read = await stream.ReadAsync(buffer, cancellationToken);
```

## Allocations and pooling

Each allocation is cheap, but the garbage collector has to find and reclaim it later, and that pauses your threads. Fewer allocations on a hot path means fewer and shorter pauses. To see them, add `[MemoryDiagnoser]` to a benchmark (its Allocated column), or watch `dotnet.gc.heap.total_allocated` in `dotnet-counters`.

**Pooled buffers.** `ArrayPool<T>.Shared.Rent` hands you an array that someone gave back, and `Return` gives it back again. A rented array can be bigger than you asked for, and holds whatever the last renter left, so use only the part you asked for. 100 000 bytes is over the 85 000-byte limit, so a new array of that size goes on the large object heap, which is only collected with Gen2:

| Method   | Size   |        Mean | Ratio |    Gen0 |    Gen2 | Allocated |
| -------- | ------ | ----------: | ----: | ------: | ------: | --------: |
| NewArray | 1024   |    376.6 ns |  1.00 |  0.1249 |       - |    1048 B |
| Pooled   | 1024   |    319.8 ns |  0.85 |       - |       - |         - |
| NewArray | 100000 | 40,024.8 ns |  1.00 | 31.1890 | 31.1890 |  100045 B |
| Pooled   | 100000 | 32,157.7 ns |  0.80 |       - |       - |         - |

**Building strings.** Concatenating in a loop (`result = result + "," + sku`) copies the whole string so far every time round. `StringBuilder` grows one buffer. When you can work out the length first, `string.Create` writes the characters once, straight into the new string. Joining 100 SKUs:

| Method      |       Mean | Ratio | Allocated | Alloc Ratio |
| ----------- | ---------: | ----: | --------: | ----------: |
| Concatenate | 3,243.2 ns |  1.00 |  69.56 KB |        1.00 |
| Builder     |   439.3 ns |  0.14 |   3.87 KB |        0.06 |
| Create      |   273.8 ns |  0.08 |   1.38 KB |        0.02 |

**JSON.** By default, `System.Text.Json` works out each type's shape by reflection (once, and then caches it). A source-generated `JsonSerializerContext` has it at build time, and works with trimming and Native AOT:

```csharp title="From the benchmark project: Json.cs"
[JsonSerializable(typeof(Order))]
internal sealed partial class OrderJsonContext : JsonSerializerContext;
```

```csharp
var json = JsonSerializer.Serialize(order, OrderJsonContext.Default.Order);
```

| Method          |     Mean | Ratio | Allocated | Alloc Ratio |
| --------------- | -------: | ----: | --------: | ----------: |
| Reflection      | 733.3 ns |  1.00 |   1.31 KB |        1.00 |
| SourceGenerated | 420.3 ns |  0.57 |   1.01 KB |        0.77 |

To use it in ASP.NET Core, add the context to the app's JSON options: `builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, OrderJsonContext.Default));`.

**Frozen collections.** `FrozenDictionary<TKey, TValue>` and `FrozenSet<T>` can't change after they're made (`ToFrozenDictionary()`, `ToFrozenSet()`). They spend longer building themselves so that they can read faster, so they suit data that's loaded once and read on every request. But measure before you reach for one. Looking up 100 short `SKU-n` keys, it barely helped:

| Method     |     Mean |   Error |  StdDev | Ratio | Allocated |
| ---------- | -------: | ------: | ------: | ----: | --------: |
| Dictionary | 399.7 ns | 2.13 ns | 1.99 ns |  1.00 |         - |
| Frozen     | 391.0 ns | 2.60 ns | 2.31 ns |  0.98 |         - |

A 2% gain is within what a different machine or key set would change. What a frozen collection always gives you is that nobody can change it, which is worth having on its own for shared lookup data.

## Caching

### HybridCache

`HybridCache` (package `Microsoft.Extensions.Caching.Hybrid`) keeps values in memory, and optionally in a distributed second level, such as Redis, which you add with `AddStackExchangeRedisCache`. Its main trick is **stampede protection**: when many requests miss the same key at once, only one of them runs the factory, and the rest wait for its result.

```csharp
builder.Services.AddHybridCache();

app.MapGet("/prices/{sku}", async (string sku, HybridCache cache, IPriceList prices, CancellationToken cancellationToken) =>
    await cache.GetOrCreateAsync(
        $"price:{sku}",
        async token => await prices.GetPriceAsync(sku, token),
        new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
        tags: ["prices"],
        cancellationToken: cancellationToken));
```

With a factory that took 200 ms, **50 concurrent requests for one key made 1 factory call**, and one more request afterwards still made none.

Entries expire after `Expiration`. To remove them before then, `RemoveAsync(key)` removes one, and `RemoveByTagAsync("prices")` removes every entry with that tag. Each made the next request call the factory again. Removal by tag works with the in-memory level alone.

### Output caching

Output caching stores whole responses, and serves them without running the endpoint:

```csharp
builder.Services.AddOutputCache();
app.UseOutputCache();

app.MapGet("/catalogue", () => Catalogue.Skus)
    .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)).Tag("catalogue"));
```

To tell that a response came from the cache, look for an **`Age` header**. The first request to an endpoint whose body counted how often it ran said `built 1 times`, with no `Age`. The same request 1.1 s later said `built 1 times` again, with `Age: 1`. When the data changes, evict by tag with `IOutputCacheStore` from DI: `await store.EvictByTagAsync("catalogue", cancellationToken);`. The next request said `built 3 times`. (`built 2 times` was the authenticated request in the [pitfalls](#pitfalls).)

Policies can vary the cache by query, header or route value (`SetVaryByQuery`, `SetVaryByHeader`), and `AddOutputCache(options => options.AddPolicy("…", …))` names one for several endpoints.

## Benchmarking

A `Stopwatch` round a loop misleads in three ways:

- **JIT warm-up:** the first calls include compiling the method.
- **Tiering:** .NET compiles quickly first, then recompiles hot methods with optimizations, so the speed changes mid-loop.
- **Dead code:** if the result isn't used, the JIT can skip the work entirely.

A Debug build misleads too: it's unoptimized. BenchmarkDotNet deals with the first three: it warms up, runs enough iterations to be sure, uses each result, and reports the spread. For the last, it refuses: started from a Debug build, it runs nothing, and says `Assembly Benchmarks which defines benchmarks is non-optimized`. So always start it with `-c Release`.

```csharp title="From the benchmark project: Lookups.cs"
[MemoryDiagnoser]
public class Lookups
{
    private string[] _keys = [];
    private Dictionary<string, long> _dictionary = [];
    private FrozenDictionary<string, long> _frozen = FrozenDictionary<string, long>.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _keys = [.. Enumerable.Range(1, 100).Select(i => $"SKU-{i}")];
        _dictionary = _keys.ToDictionary(key => key, key => (long)key.Length * 100);
        _frozen = _dictionary.ToFrozenDictionary();
    }

    [Benchmark(Baseline = true)]
    public long Dictionary()
    {
        var total = 0L;
        foreach (var key in _keys)
        {
            total += _dictionary[key];
        }

        return total;
    }

    [Benchmark]
    public long Frozen()
    {
        var total = 0L;
        foreach (var key in _keys)
        {
            total += _frozen[key];
        }

        return total;
    }
}
```

- `[GlobalSetup]` makes the inputs once, outside the measurement. Keeping them in fields, not constants, stops the JIT from working the answer out at compile time.
- Each benchmark returns its result, so that the work can't be skipped.
- `Baseline = true` gives the Ratio column. `[MemoryDiagnoser]` adds Gen0 to Gen2 (collections per 1000 calls) and Allocated (bytes per call).

`Program.cs` runs `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args)`, and you always run it in Release:

```bash
dotnet run -c Release --project samples/benchmarks/Benchmarks -- --filter '*Lookups*'
```

Reading the results: **Mean** is the average time per call. **Error** is half the 99.9% confidence interval: the true mean is very probably within Mean ± Error, though not certainly, and BenchmarkDotNet warns when the timings don't look normally distributed. **StdDev** is how much single runs varied. When two Means are closer than their Errors, as in the frozen-dictionary table, there's no proven difference. **Allocated** is the managed memory one call allocated, and `-` means none.

The full project, with all five benchmarks on this page, is [`samples/benchmarks/Benchmarks`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/benchmarks/Benchmarks). The gates build it, but never run it.

## Coming from .NET 3.5

| In .NET 3.5                                                                     | Now                                                                                      |
| ------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `BeginXxx`/`EndXxx` and `IAsyncResult`                                          | `async` methods and [`await`](#async-end-to-end)                                         |
| The event-based pattern: `XxxAsync` with `XxxCompleted`, and `BackgroundWorker` | `await`, with `IProgress<T>` for progress                                                |
| `ThreadPool.QueueUserWorkItem`                                                  | `Task.Run`, for CPU-bound work only, and never in a request to make sync code look async |
| `new Thread(…)` for background work                                             | A [`BackgroundService`](#blocking-async-void-and-fire-and-forget)                        |
| `HttpRuntime.Cache` and `Cache.Insert`                                          | [`HybridCache`](#hybridcache), or [output caching](#output-caching)                      |
| `string` concatenation in a loop                                                | [`StringBuilder`, or `string.Create`](#allocations-and-pooling) when the length is known |
| `Thread.Sleep` to wait                                                          | `await Task.Delay(…, cancellationToken)`                                                 |
| Timing with `DateTime.Now` round a loop                                         | [BenchmarkDotNet](#benchmarking)                                                         |

## Pitfalls

- **The app slows to a crawl under load, but CPU is low.** Something blocks on a task: `.Result`, `.Wait()` or `.GetAwaiter().GetResult()`, so each request holds a thread while it waits, and the pool starves. Fix: `await` it, async all the way up. Find it with [`dotnet-trace`](#find-the-blocking-call-with-dotnet-trace).
- **Work carries on after the caller has gone.** The endpoint takes a `CancellationToken`, but doesn't pass it down, so the call that waits gets `CancellationToken.None`. Fix: pass the token to every call that takes one.
- **The process dies from an exception in a background method.** It's `async void`, so the exception had nowhere to go. Fix: `async Task`, awaited, and `async void` for event handlers only.
- **A failure never shows up in the logs.** The task was discarded (`_ = SendAsync()`), so its exception went nowhere. Fix: hand the work to a [hosted service](#blocking-async-void-and-fire-and-forget) that awaits it and logs failures.
- **CS4007 or CS8345 on a span.** A `Span<T>` was in use across an `await` (CS4007), or declared as a field of a class (CS8345). Fix: keep a `Memory<T>`, and take `.Span` between awaits.
- **A speed-up that doesn't hold up.** It was timed with a `Stopwatch` loop, or in a Debug build, so warm-up, tiering or skipped work decided the result. Fix: a [BenchmarkDotNet](#benchmarking) class, run in Release.
- **Output caching does nothing for signed-in users.** By default it doesn't serve or store an authenticated request (one with an `Authorization` header, or a signed-in user), or a response that sets a cookie: a request with `Authorization: Bearer …` said `built 2 times`, with no `Age`. That's on purpose, so that one user's response isn't served to another. Fix: cache data per user with `HybridCache`, or write an output cache policy that varies by user, if you're sure it's safe.

## Exercise 11: unblock the orders API

**Goal:** fix an orders API that gives the right answers, but wastes threads, keeps working for callers who have gone, loses failures, and makes callers wait longer than they need to. `POST /orders` reserves stock with a slow warehouse and then tells it about the order; `GET /orders/{id}/quote` asks the warehouse for stock, price and shipping. The store, the validation, the warehouse and a notification queue with its worker are already written. The tests swap in a fake warehouse that they control.

**Where:** [`samples/exercises/11-async`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/11-async) in the GitHub repo. Clone the repo, then work in the `Starter` project. **This exercise needs no Docker**: the store and the warehouse are in memory, and the tests run the app in process.

**Steps:**

1. In `Starter/Program.cs`, make `POST /orders` async, and await the reservation instead of blocking on it. Keep the work in the handler (TODO 1).
2. Take the endpoint's `CancellationToken`, and pass it to `ReserveAsync` (TODO 2).
3. Instead of discarding the notification's task, hand the order to the `OrderNotifications` queue, whose worker awaits it and logs a failure (TODO 3).
4. In `GET /orders/{id}/quote`, start the three warehouse calls together, await `Task.WhenAll`, then read each result with `await` (TODO 4).

**Check your work:** the tests start red and turn green as you go.

```sh
dotnet test --project samples/exercises/11-async/Starter.Tests
```

Each failure names the TODO to look at. No test times anything: the blocking test reads the handler's compiled code, and the others wait for the fake warehouse to see a cancellation, an error log or three calls at once. So a fix passes on a slow machine too.

**Try the running app:** start it with `dotnet run --project samples/exercises/11-async/Starter`. Each warehouse call takes half a second, so a quote takes 1.5 s before TODO 4, and 0.5 s after it.

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
