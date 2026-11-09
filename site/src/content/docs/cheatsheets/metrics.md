---
title: Metrics cheatsheet
description: Instruments and when to use each, IMeterFactory, recording with tags, built-in meters, dotnet-counters, and semantic-convention names and units, on one page.
prev: false
next: false
learn:
  - title: .NET metrics
    url: https://learn.microsoft.com/dotnet/core/diagnostics/metrics
  - title: Creating metrics
    url: https://learn.microsoft.com/dotnet/core/diagnostics/metrics-instrumentation
  - title: ASP.NET Core metrics
    url: https://learn.microsoft.com/aspnet/core/metrics/overview
  - title: Investigate performance counters (dotnet-counters)
    url: https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-counters
---

Back to [Metrics](/observability/metrics/).

## Instruments

| Instrument                   | Use for                                             | Example                       |
| ---------------------------- | --------------------------------------------------- | ----------------------------- |
| `Counter<T>`                 | things that only go up; the backend works out rates | orders placed, bytes sent     |
| `UpDownCounter<T>`           | a running total that goes up and down               | requests in flight, open jobs |
| `Histogram<T>`               | a distribution: percentiles, not just averages      | request duration, order value |
| `Gauge<T>` (.NET 9+)         | the current value, set when it changes              | the last reading of a sensor  |
| `ObservableCounter<T>`       | a total you can read, but not count as it happens   | CPU time used so far          |
| `ObservableUpDownCounter<T>` | a level you can read, that goes up and down         | items in a cache              |
| `ObservableGauge<T>`         | a sampled value, read when metrics are collected    | queue length, free disk space |

Observable instruments take a callback that's called at each collection.

## Create a meter

```csharp title="OrderMetrics.cs"
using System.Diagnostics.Metrics;

internal sealed class OrderMetrics
{
    private readonly Counter<long> _ordersPlaced;
    private readonly Histogram<double> _orderValue;

    public OrderMetrics(IMeterFactory meterFactory)              // injected; registered by default
    {
        var meter = meterFactory.Create("Orders");
        _ordersPlaced = meter.CreateCounter<long>("orders.placed", unit: "{order}");
        _orderValue = meter.CreateHistogram<double>("orders.value", unit: "{GBP}");
    }
}
```

```csharp title="Program.cs"
builder.Services.AddSingleton<OrderMetrics>();
```

## Record with tags

```csharp
_ordersPlaced.Add(1,
    new KeyValuePair<string, object?>("payment.method", "card"),
    new KeyValuePair<string, object?>("order.outcome", "accepted"));

_orderValue.Record(42.50, new KeyValuePair<string, object?>("payment.method", "card"));

// More than three tags: a TagList, which doesn't allocate for up to eight.
var tags = new TagList { { "payment.method", "card" }, { "order.outcome", "accepted" } };
_ordersPlaced.Add(1, tags);
```

```csharp
// Observable: a callback, read at each collection.
meter.CreateObservableGauge("orders.queue.length", () => queue.Count, unit: "{order}");
```

**Tags must be bounded:** methods, outcomes, status classes. Never ids, emails, URLs with ids in
them, or free text. Each distinct combination is a separate time series.

## Export

```csharp title="Program.cs"
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("Orders")                       // your own meters, by name
        .AddAspNetCoreInstrumentation()           // ASP.NET Core's meters, Kestrel's included
        .AddHttpClientInstrumentation()
        .AddMeter("System.Runtime"))
    .UseOtlpExporter();
```

Packages and the rest of the setup: the
[Logging page](/observability/logging/#one-setup-for-all-three).

## Built-in meters

| Meter                                 | Instruments (some)                                                                                   |
| ------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| `Microsoft.AspNetCore.Hosting`        | `http.server.request.duration` (s), `http.server.active_requests`                                    |
| `Microsoft.AspNetCore.Server.Kestrel` | `kestrel.active_connections`, `kestrel.connection.duration` (s), `kestrel.queued_connections`        |
| `System.Net.Http`                     | `http.client.request.duration` (s), `http.client.open_connections`                                   |
| `System.Runtime` (.NET 9+)            | `dotnet.gc.collections`, `dotnet.process.memory.working_set` (By), `dotnet.thread_pool.queue.length` |

## Watch live

```sh
dnx dotnet-counters ps                    # find your app's process name or id
# -n takes the process name from ps; -p takes the id
dnx dotnet-counters monitor -n OrdersApi \
  --counters Orders,Microsoft.AspNetCore.Hosting,Microsoft.AspNetCore.Server.Kestrel,System.Runtime
```

`dnx` (.NET 10) runs the tool without installing it; for the tool's own `--help`, use
`dnx dotnet-counters -- monitor --help`. Without `--counters`, you see only `System.Runtime`. Only one
`dotnet-counters` session can attach to a process at a time.

## Names and units

| Measuring        | Name (example)                 | Unit      |
| ---------------- | ------------------------------ | --------- |
| durations        | `http.server.request.duration` | `s`       |
| sizes            | `orders.payload.size`          | `By`      |
| counts of things | `orders.placed`                | `{order}` |
| money            | `orders.value`                 | `{GBP}`   |
| ratios           | `cache.hit_ratio`              | `1`       |

Dotted, lower case, `_` between words, no unit in the name. Units follow
[UCUM](https://ucum.org/ucum); a name in braces is an annotation, for things UCUM has no unit for.
