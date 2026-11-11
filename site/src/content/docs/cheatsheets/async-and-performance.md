---
title: Async & performance cheatsheet
description: Async endpoints, tokens and timeouts, blocking calls to replace, background work, dotnet-counters and dotnet-trace, ValueTask, spans, pooling, caching and BenchmarkDotNet, on one page.
prev: false
next: false
learn:
  - title: Asynchronous programming with async and await
    url: https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/
  - title: ASP.NET Core best practices
    url: https://learn.microsoft.com/aspnet/core/fundamentals/best-practices
  - title: dotnet-counters
    url: https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-counters
  - title: dotnet-trace
    url: https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-trace
  - title: Memory- and span-related types
    url: https://learn.microsoft.com/dotnet/standard/memory-and-spans/
  - title: HybridCache library in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/performance/caching/hybrid
  - title: Output caching middleware in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/performance/caching/output
---

Back to [Async & performance](/performance/async-and-performance/).

## Async endpoints

```csharp title="Program.cs"
// Async all the way: await the slow call, and pass the request's token to it.
// Minimal APIs bind a CancellationToken parameter to HttpContext.RequestAborted.
app.MapPost("/orders", async (PlaceOrderRequest request, IWarehouse warehouse, CancellationToken cancellationToken) =>
{
    await warehouse.ReserveAsync(request.Lines!, cancellationToken);
    return Results.Accepted();
});
```

```csharp title="Independent calls: start them all, then wait for them together"
var stock = warehouse.GetStockAsync(order.Lines, cancellationToken);
var price = warehouse.GetPriceAsync(order.Lines, cancellationToken);
var shipping = warehouse.GetShippingAsync(order.Lines, cancellationToken);
await Task.WhenAll(stock, price, shipping);

// They're done, so these awaits don't wait. Use await, not .Result.
var quote = new Quote(order.Id, await stock, await price, await shipping);
```

## Timeouts

```csharp title="A deadline for one call, on top of the caller's token"
using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
deadline.CancelAfter(TimeSpan.FromSeconds(2));
await warehouse.ReserveAsync(lines, deadline.Token);    // OperationCanceledException after 2 s
```

```csharp title="A deadline for a whole request"
builder.Services.AddRequestTimeouts();
app.UseRequestTimeouts();

app.MapGet("/report", async (CancellationToken cancellationToken) => { /* … */ })
    .WithRequestTimeout(TimeSpan.FromSeconds(5));   // cancels RequestAborted; not under a debugger
```

## Blocking calls and their replacements

| Blocks a thread                         | Instead                                    |
| --------------------------------------- | ------------------------------------------ |
| `task.Result`, `task.Wait()`            | `await task`                               |
| `task.GetAwaiter().GetResult()`         | `await task`                               |
| `Thread.Sleep(500)`                     | `await Task.Delay(500, cancellationToken)` |
| `Task.WaitAll(a, b)`                    | `await Task.WhenAll(a, b)`                 |
| `File.ReadAllText(path)`                | `await File.ReadAllTextAsync(path, token)` |
| `lock` around slow work                 | `await semaphore.WaitAsync(token)`         |
| `_ = SendAsync()` (its failure is lost) | a queue and a `BackgroundService`          |
| `async void` (a throw ends the process) | `async Task`, awaited                      |

## Background work

```csharp title="OrderNotifications.cs: a queue, and a worker that awaits each item, and drains on stop"
public sealed class OrderNotifications
{
    private readonly Channel<Order> _orders = Channel.CreateUnbounded<Order>();

    public void Add(Order order)
    {
        if (!_orders.Writer.TryWrite(order))       // only once Complete has been called
        {
            throw new InvalidOperationException("The app is stopping.");
        }
    }

    public void Complete() => _orders.Writer.TryComplete();

    public IAsyncEnumerable<Order> ReadAllAsync() => _orders.Reader.ReadAllAsync();   // ends once completed and empty
}

public sealed partial class NotificationWorker(
    OrderNotifications notifications, IWarehouse warehouse, ILogger<NotificationWorker> logger) : BackgroundService
{
    private readonly CancellationTokenSource _outOfTime = new();   // not stoppingToken: that's cancelled as stopping starts

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var order in notifications.ReadAllAsync())
        {
            try
            {
                _outOfTime.Token.ThrowIfCancellationRequested();
                await warehouse.NotifyAsync(order, _outOfTime.Token);
            }
            catch (Exception exception)
            {
                LogNotifyFailed(exception, order.Id);    // seen, and the worker carries on
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        notifications.Complete();                                    // send what's queued, then stop
        using var _ = cancellationToken.Register(_outOfTime.Cancel); // until the shutdown timeout
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _outOfTime.Dispose();
        base.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Notifying the warehouse about order {OrderId} failed.")]
    private partial void LogNotifyFailed(Exception exception, int orderId);
}
```

An in-memory queue is lost in a crash. When the work mustn't be lost, use a durable queue, such as Amazon SQS.

```csharp title="Program.cs"
builder.Services.AddSingleton<OrderNotifications>();
builder.Services.AddHostedService<NotificationWorker>();
```

## dotnet-counters and dotnet-trace

```bash title="Install, then find the process"
dotnet tool install -g dotnet-counters
dotnet tool install -g dotnet-trace
dotnet-counters ps
```

```bash title="Watch the thread pool live"
dotnet-counters monitor -n Async.Solution --counters System.Runtime
```

Starvation shows as a rising `dotnet.thread_pool.thread.count` with a non-zero `dotnet.thread_pool.queue.length`.

```bash title="Record counters to a file instead"
dotnet-counters collect -p <pid> --counters System.Runtime --format csv -o counters.csv --duration 00:00:30
```

```bash title="Trace, then find where the time goes"
dotnet-trace collect -p <pid> --duration 00:00:00:10 -o blocking.nettrace
dotnet-trace report blocking.nettrace topN -n 25 --inclusive
```

Read the `--inclusive` view: a blocked request shows up as `Task.InternalWait` under `EndpointMiddleware.Invoke`. The default, exclusive view leads with `LowLevelLifoSemaphore.WaitNative`, which is idle pool threads waiting for work.

## ValueTask

```csharp title="For a result that's usually ready already"
public ValueTask<long> GetPriceAsync(string sku, CancellationToken cancellationToken) =>
    _prices.TryGetValue(sku, out var price)
        ? ValueTask.FromResult(price)                                  // no Task allocated
        : new ValueTask<long>(LoadPriceAsync(sku, cancellationToken));
```

- Await a `ValueTask` once, and only once.
- Don't read `.Result` until it has completed. Don't pass it to `Task.WhenAll`: call `.AsTask()` first.
- Return `Task` unless a measurement says the allocation matters.

## Spans, Memory and stackalloc

```csharp title="Read part of a string where it is"
ReadOnlySpan<char> line = "SKU-1,2,1250";
var comma = line.IndexOf(',');
ReadOnlySpan<char> sku = line[..comma];                  // no new string
var rest = line[(comma + 1)..];
var quantity = int.Parse(rest[..rest.IndexOf(',')], CultureInfo.InvariantCulture);
```

```csharp title="Small, fixed-size scratch space on the stack"
Span<byte> scratch = stackalloc byte[256];    // the thread's stack: keep it small and fixed
```

```csharp title="Across an await, use Memory<T>"
Memory<byte> buffer = new byte[4096];
var read = await stream.ReadAsync(buffer, cancellationToken);   // a Span<T> can't live across an await
```

## Pooling and fast APIs

```csharp title="Rent a buffer instead of allocating one"
var buffer = ArrayPool<byte>.Shared.Rent(size);    // can be bigger than size, and isn't cleared
try
{
    // use buffer.AsSpan(0, size)
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

```csharp title="Build a string of known length in one go"
var joined = string.Create(length, skus, static (span, skus) =>
{
    var at = 0;
    foreach (var sku in skus)
    {
        if (at > 0)
        {
            span[at++] = ',';
        }

        sku.AsSpan().CopyTo(span[at..]);
        at += sku.Length;
    }
});
```

```csharp title="Source-generated JSON: no reflection, and it works with trimming and AOT"
[JsonSerializable(typeof(Order))]
internal sealed partial class OrderJsonContext : JsonSerializerContext;

var json = JsonSerializer.Serialize(order, OrderJsonContext.Default.Order);
```

```csharp title="Frozen collections: slower to build, for data that's read and never changed"
FrozenDictionary<string, long> prices = loaded.ToFrozenDictionary();
```

## Caching

```csharp title="HybridCache: one factory call, however many requests miss at once"
// Package: Microsoft.Extensions.Caching.Hybrid
builder.Services.AddHybridCache();

app.MapGet("/prices/{sku}", async (string sku, HybridCache cache, IPriceList prices, CancellationToken cancellationToken) =>
    await cache.GetOrCreateAsync(
        $"price:{sku}",
        async token => await prices.GetPriceAsync(sku, token),
        new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
        tags: ["prices"],
        cancellationToken: cancellationToken));
```

```csharp title="Throw entries away"
await cache.RemoveAsync($"price:{sku}", cancellationToken);
await cache.RemoveByTagAsync("prices", cancellationToken);     // works with the in-memory level alone
```

```csharp title="Output caching: whole responses"
builder.Services.AddOutputCache();
app.UseOutputCache();

app.MapGet("/catalogue", () => Catalogue.Skus)
    .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)).Tag("catalogue"));

// Later, when the catalogue changes (IOutputCacheStore from DI):
await store.EvictByTagAsync("catalogue", cancellationToken);
```

A cached response carries an `Age` header. By default, an authenticated request (an `Authorization` header, or a signed-in user) isn't served from the cache or stored in it, and neither is a response that sets a cookie.

## BenchmarkDotNet

```csharp title="Lookups.cs"
// Package: BenchmarkDotNet
[MemoryDiagnoser]
public class Lookups
{
    private string[] _keys = [];
    private Dictionary<string, long> _dictionary = [];
    private FrozenDictionary<string, long> _frozen = FrozenDictionary<string, long>.Empty;

    [GlobalSetup]       // inputs in fields, so the JIT can't fold them away
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

        return total;   // return the result, so the work can't be skipped
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

```csharp title="Program.cs"
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```

```bash title="Always in Release"
dotnet run -c Release --project samples/benchmarks/Benchmarks -- --filter '*'
dotnet run -c Release --project samples/benchmarks/Benchmarks -- --filter '*Lookups*'
```
