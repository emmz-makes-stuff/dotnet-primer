---
title: "Metrics"
description: "Metrics with System.Diagnostics.Metrics and OpenTelemetry: when a metric is the right signal, choosing an instrument, names, units and low-cardinality tags, the built-in meters, and watching them live."
learn:
  - title: ".NET metrics"
    url: https://learn.microsoft.com/dotnet/core/diagnostics/metrics
  - title: "Creating metrics"
    url: https://learn.microsoft.com/dotnet/core/diagnostics/metrics-instrumentation
  - title: "ASP.NET Core metrics"
    url: https://learn.microsoft.com/aspnet/core/metrics/overview
  - title: "Investigate performance counters (dotnet-counters)"
    url: https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-counters
---

By the end of this page you'll know when a metric is the right signal, be able to pick the right instrument for a measurement, name it and give it a unit the way backends expect, keep its tags from exploding into millions of time series, and watch your app's own and built-in metrics live.

:::tip[Cheatsheet]
Keep the [Metrics cheatsheet](/cheatsheets/metrics/) open while you work.
:::

Building on the setup from [Logging](/observability/logging/#one-setup-for-all-three): that page's `WithMetrics(…)` call already exports ASP.NET Core's, `HttpClient`'s and the runtime's built-in metrics to the local dashboard. This page adds your own.

## When a metric is the right signal

A metric is a number aggregated over time: a count, a sum, a distribution. It answers "how often?", "how much?" and "how is it trending?", cheaply, for every request, forever. That's what you graph and alert on. It can't tell you about one request; that's what [logs](/observability/logging/) and [traces](/observability/tracing/) are for.

Two checklists say what to measure first:

- **RED, for anything that serves requests:** **R**ate (requests per second), **E**rrors (failed requests per second), and **D**uration (how long they take, as a distribution). ASP.NET Core's built-in `http.server.request.duration` gives you all three for HTTP: it's a histogram, so it has a count (the rate), and its `http.response.status_code` and `error.type` tags give you the errors.
- **USE, for resources:** **U**tilisation (how busy), **S**aturation (how much work is waiting), and **E**rrors. For a .NET process, that's CPU and memory, the thread pool's queue length, and the GC. The runtime's built-in meter covers it.

So the built-in metrics answer the generic questions. Add your own for the **business**: orders placed, payments declined, the value of orders, the length of a queue only your app knows about.

## Choosing an instrument

Pick the instrument by the shape of the measurement, not by what's convenient to call:

| You're measuring                                             | Instrument                           | Why                                                                                               |
| ------------------------------------------------------------ | ------------------------------------ | ------------------------------------------------------------------------------------------------- |
| something that only goes up (orders, bytes, errors)          | `Counter<T>`                         | You add to it; the backend works out the rate, and copes with restarts.                           |
| something in progress (requests, jobs in flight)             | `UpDownCounter<T>`                   | It goes up when work starts and down when it ends.                                                |
| a distribution (durations, sizes, order values)              | `Histogram<T>`                       | You need percentiles: an average hides the slow 1%.                                               |
| a value you sample (queue length, free disk, a cache's size) | `ObservableGauge<T>` (or `Gauge<T>`) | The value exists already; you read it when metrics are collected, rather than track every change. |

So for the three common cases: **request durations** are a histogram, **a queue's length** is an observable gauge, and **a count of orders** is a counter.

Every synchronous instrument has an observable form that takes a callback, which is called at each collection: `ObservableCounter<T>` for a total you can read but not count as it happens (such as CPU time from the OS), `ObservableUpDownCounter<T>`, and `ObservableGauge<T>`. `Gauge<T>` (.NET 9 and later) is the synchronous gauge, for when you're told the new value, rather than reading it.

The common mistakes: a counter for a duration (you get a total, not a distribution), a gauge for a count (you lose everything between samples), and an up-down counter for something that only goes up.

## Naming and units

Name instruments the way the [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/general/metrics/) do, so that backends and dashboards understand them:

- **Dotted, lower case**, from general to specific, with `_` between words: `orders.placed`, `orders.value`, `http.server.request.duration`.
- **Use the standard name where one exists.** Don't invent `orders_api_request_time_ms` when `http.server.request.duration` already exists and is built in.
- **The unit goes on the instrument, not in the name:** `http.server.request.duration` with unit `s`, never `…duration_seconds`.
- **Units are [UCUM](https://ucum.org/ucum) codes:** `s` for seconds (durations are in seconds, as `double`), `By` for bytes, `1` for a ratio.
- **A count of things has a curly-brace annotation** as its unit, such as `{order}` or `{request}`, because "orders" isn't a unit of measure.

Money is a judgement call: UCUM has no currency units. This site uses an annotation, `{GBP}`, which is the form OpenTelemetry recommends for anything UCUM doesn't cover. Whatever you choose, state it on the instrument, so that nobody has to guess whether `orders.value` is in pounds or pence.

## Keeping cardinality low

Tags (OpenTelemetry calls them attributes) let you break a metric down: orders placed **by payment method**, requests **by route and status code**. But a metric backend stores a separate **time series for every distinct combination of tag values**, and that's what you pay for and query.

So every tag must have a small, known set of values:

- **Do:** `payment.method` (card, voucher, other), `order.outcome` (accepted, declined, failed), `http.response.status_code`, `http.route`.
- **Don't:** an order id, a user id, an email, a full URL, an exception message, a timestamp: anything unbounded.

Tag `orders.placed` with the order id, and every order makes a new time series that's used once. A million orders is a million series: the backend slows down or starts dropping data, the bill grows, and the graph is useless anyway, because each series has one point. If you want to find one order, that's a job for a [log entry](/observability/logging/) or a [trace attribute](/observability/tracing/#choosing-attributes), where high cardinality is fine.

Watch for values that **look** bounded but aren't. In Exercise 06, the payment method comes from the client, so a buggy or hostile client could send a new one with every request. The exercise maps it to a fixed set before tagging:

```csharp
internal static string PaymentMethodTag(string paymentMethod) =>
    paymentMethod.ToUpperInvariant() switch
    {
        "CARD" => "card",
        "VOUCHER" => "voucher",
        _ => "other",
    };
```

## Creating meters

A **meter** is a named group of instruments, usually one per component. Create it through **`IMeterFactory`**, which ASP.NET Core registers for you. Meters from the factory are tied to the app's service provider, so they're disposed with it, and tests can listen to one app's meters without seeing another's. This is `OrderMetrics.cs` from Exercise 06:

```csharp title="OrderMetrics.cs"
using System.Diagnostics.Metrics;

namespace Orders;

/// <summary>
/// The app's metrics. The meter comes from <see cref="IMeterFactory"/>, so it's tied to the app's
/// lifetime and tests can listen to it; OpenTelemetry subscribes to it by name
/// (<c>.AddMeter("Orders")</c>). Register this class as a singleton.
/// </summary>
internal sealed class OrderMetrics
{
    public const string MeterName = "Orders";

    private readonly Counter<long> _ordersPlaced;
    private readonly Histogram<double> _orderValue;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        // A counter: it only goes up, and the backend works out the rate. The unit is a
        // curly-brace annotation, because "orders" isn't a unit of measure.
        _ordersPlaced = meter.CreateCounter<long>(
            "orders.placed", unit: "{order}", description: "Orders placed, by payment method and outcome.");

        // A histogram: a distribution, so you can ask for the median or the 95th percentile.
        // There's no standard currency unit, so the currency goes in an annotation too.
        _orderValue = meter.CreateHistogram<double>(
            "orders.value", unit: "{GBP}", description: "The value of accepted orders.");
    }

    /// <summary>Counts an order, whatever happened to it.</summary>
    public void OrderPlaced(string paymentMethod, string outcome) =>
        _ordersPlaced.Add(
            1,
            new KeyValuePair<string, object?>("payment.method", PaymentMethodTag(paymentMethod)),
            new KeyValuePair<string, object?>("order.outcome", outcome));

    /// <summary>Records an accepted order's value.</summary>
    public void OrderAccepted(string paymentMethod, decimal total) =>
        _orderValue.Record(
            (double)total,
            new KeyValuePair<string, object?>("payment.method", PaymentMethodTag(paymentMethod)));

    /// <summary>
    /// The payment method as a tag value. It comes from the client, so it could be anything: map it
    /// to a fixed set, or every new value becomes a new time series.
    /// </summary>
    internal static string PaymentMethodTag(string paymentMethod) =>
        paymentMethod.ToUpperInvariant() switch
        {
            "CARD" => "card",
            "VOUCHER" => "voucher",
            _ => "other",
        };
}
```

Register the class as a singleton, so that its instruments are created once, and inject it where you record:

```csharp title="Program.cs"
builder.Services.AddSingleton<OrderMetrics>();
```

Wrapping the instruments in a class like this keeps the names, units and allowed tags in one place, and gives callers methods that can't be called with the wrong tags.

## Recording with tags

Each recording call takes the value and its tags as `KeyValuePair<string, object?>`s:

```csharp
metrics.OrderPlaced(order.PaymentMethod, "accepted");      // Add(1, payment.method, order.outcome)
metrics.OrderAccepted(order.PaymentMethod, order.Total);   // Record(total, payment.method)
```

Up to three tags have their own overloads. For more, use a `TagList`, a struct that doesn't allocate for up to eight tags:

```csharp
var tags = new TagList { { "payment.method", "card" }, { "order.outcome", "accepted" } };
ordersPlaced.Add(1, tags);
```

An observable instrument takes a callback instead, which is called at each collection, so there's nothing to record:

```csharp
meter.CreateObservableGauge("orders.queue.length", () => queue.Count, unit: "{order}");
```

Recording is cheap, and it's nearly free when nothing is listening, so you don't need to guard it.

### Registering the meter

As with trace sources, OpenTelemetry only collects the meters you name. Add yours to the setup:

```csharp title="Program.cs"
.WithMetrics(metrics => metrics
    .AddMeter(OrderMetrics.MeterName)
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddMeter("System.Runtime"))
```

Histograms are exported as counts in **buckets**. OpenTelemetry's default bucket boundaries run from 0 to 10,000, which suits values like milliseconds or pounds, but not durations in seconds. ASP.NET Core sets suitable boundaries for its own durations. For your own histograms, set them with a view:

```csharp
metrics.AddView("orders.value",
    new ExplicitBucketHistogramConfiguration { Boundaries = [5, 10, 25, 50, 100, 250, 500, 1000] });
```

## The built-in metrics

.NET publishes metrics of its own on built-in meters, named after the component. These are what this site's setup collects, as seen in a running app at the time of writing:

| Meter                                 | Instruments                                                                                                                                                                                                  |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Microsoft.AspNetCore.Hosting`        | `http.server.request.duration` (s), tagged with method, route, status code and `error.type`; `http.server.active_requests`                                                                                   |
| `Microsoft.AspNetCore.Server.Kestrel` | `kestrel.active_connections`, `kestrel.connection.duration` (s), `kestrel.queued_connections`                                                                                                                |
| `System.Net.Http`                     | `http.client.request.duration` (s), `http.client.active_requests`, `http.client.open_connections`, `http.client.request.time_in_queue` (s)                                                                   |
| `System.Runtime`                      | `dotnet.gc.collections`, `dotnet.gc.pause.time` (s), `dotnet.process.memory.working_set` (By), `dotnet.process.cpu.time` (s), `dotnet.thread_pool.queue.length`, `dotnet.thread_pool.thread.count`, and more |

- **`AddAspNetCoreInstrumentation()`** subscribes to ASP.NET Core's meters, Kestrel's included. **`AddHttpClientInstrumentation()`** subscribes to `System.Net.Http`.
- **`System.Runtime`** is built into .NET 9 and later. Subscribe with `AddMeter("System.Runtime")`: you don't need the separate runtime instrumentation package.
- **Kestrel's instruments report on connection events.** `kestrel.active_connections` changes when a connection opens or closes, so a client that keeps one connection alive shows little there.

Learn lists them all, with their tags: [ASP.NET Core](https://learn.microsoft.com/aspnet/core/metrics/built-in) and [.NET](https://learn.microsoft.com/dotnet/core/diagnostics/built-in-metrics).

## Watching metrics live

**`dotnet-counters`** shows a running app's metrics in your terminal, with no code, no exporter and no dashboard. In .NET 10, `dnx` runs it without installing anything:

```sh
dnx dotnet-counters ps
```

That lists the .NET processes you can watch. Then monitor one by its name (or its id, with `-p`):

```sh
dnx dotnet-counters monitor -n OrdersApi \
  --counters Orders,Microsoft.AspNetCore.Hosting,Microsoft.AspNetCore.Server.Kestrel,System.Runtime
```

Send the app some requests and watch the values update: `orders.placed` broken down by `order.outcome` and `payment.method`, `orders.value` and `http.server.request.duration` as percentiles, the open connections, and the GC and thread pool. Press `q` to stop.

- **`--counters`** picks the meters. Without it, you get only `System.Runtime`. List a few at a time: the display is one screen, and a long list scrolls off it.
- **One session at a time:** a second `dotnet-counters` attached to the same process fails with "Another metrics collection session is already in progress".
- `dotnet-counters collect --format csv` (or `json`) writes the values to a file instead.
- **`dnx` reads some options itself**, such as `--help`. If you get `dnx`'s help instead of the tool's, put `--` after the tool's name: `dnx dotnet-counters -- monitor --help`.

It works against any .NET process on your machine, including one in production you can reach. That makes it the quickest check that a metric you've just added is being recorded at all.

## Viewing metrics in the dashboard

With the [dashboard running](/observability/logging/#see-it-locally), open **Metrics** at `http://localhost:18888`. Choose the `orders` resource, then an instrument such as `orders.placed`. The dashboard draws it over time, and lets you filter and split it by its tag values, so you can see declined voucher orders separately from accepted card ones. Histograms show their percentiles.

Metrics are exported every 60 seconds by default, so a new value can take up to a minute to appear. On your machine, you can shorten the interval with the `OTEL_METRIC_EXPORT_INTERVAL` environment variable, in milliseconds.

## In production

On your machine, the app sends OTLP straight to the dashboard. In production, it usually sends to an [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/) instead: a separate process, often a sidecar, that batches the telemetry, filters it (dropping a high-cardinality tag there is a useful safety net), and fans it out to one or more backends. The app only knows the collector's address, `OTEL_EXPORTER_OTLP_ENDPOINT`, so you can change backends with no code change.

**On AWS**, CloudWatch accepts OTLP directly, at its own [OTLP endpoints](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch-OTLPEndpoint.html) for traces (X-Ray), logs and metrics. At the time of writing, AWS recommends putting a collector in front of them: on ECS, the [AWS Distro for OpenTelemetry (ADOT) collector](https://aws-otel.github.io/docs/setup/ecs) as a sidecar in your task; on EKS, the CloudWatch agent, through the CloudWatch Observability add-on. Either way, the app keeps exporting plain OTLP to the collector, so none of its instrumentation code changes. The [Orchestration & AWS](/deploy/orchestration-and-aws/) page covers the deployment side.

## Pitfalls

- **An id as a tag.** Every order, user or request makes a new time series, so the backend slows, drops data or costs a fortune, and the metric can't be graphed anyway. Fix: bounded tags only, and identifiers in logs and traces. Map client-supplied values to a fixed set before tagging.
- **The wrong instrument.** A counter for durations gives a meaningless total. A gauge for a count misses everything between samples. An average of a histogram hides the slow requests. Fix: choose by the measurement's shape (see [Choosing an instrument](#choosing-an-instrument)), and look at percentiles.
- **Missing or wrong units.** Without a unit, nobody knows whether `orders.value` is in pounds or pence, or a duration in seconds or milliseconds, and backends can't convert or label it. Fix: always pass `unit:`, use UCUM codes, and record durations in seconds, as `double`.
- **A meter that isn't registered.** OpenTelemetry collects only the meters you name, so nothing is exported. Fix: `AddMeter(OrderMetrics.MeterName)`, from the same constant. `dotnet-counters` reads any meter you name in `--counters`, registered with OpenTelemetry or not, so it's the quickest way to tell whether the problem is recording or exporting.
- **`new Meter(…)` instead of `IMeterFactory`.** A static meter is shared by every app in the process, so parallel tests see each other's measurements, and nothing disposes it. Fix: create meters through the injected `IMeterFactory`.
- **Expecting values at once.** The default export interval is 60 seconds. Fix: wait, use `dotnet-counters`, or shorten `OTEL_METRIC_EXPORT_INTERVAL` locally.

## Exercise 06: metrics for an order endpoint

**Goal:** add metrics to an order endpoint that works but has none. Count every order by payment method and outcome, record the value of accepted orders in a histogram with its unit, and keep identifiers out of the tags, whatever a colleague suggests.

**Where:** [`samples/exercises/06-metrics`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/06-metrics) in the GitHub repo. Clone the repo, then work in the `Starter` project. The tests capture measurements in memory with `MetricCollector<T>`, so you don't need Docker.

**Steps:**

1. Open `Starter/OrderMetrics.cs`. Add a constructor that takes an `IMeterFactory`, creates the `Orders` meter, and creates the `orders.placed` counter and the `orders.value` histogram, each with its unit (TODO 1).
2. Fill in `OrderPlaced` and `OrderAccepted`, tagging with `PaymentMethodTag`, which is already there (TODOs 2 and 3). Then remove the `#pragma warning disable` and `restore` lines around them: they only keep the empty methods building.
3. In `Starter/Program.cs`, call `OrderPlaced` once for every order, with its outcome, and `OrderAccepted` for an accepted one (TODO 4). Read the colleague's hint there, and decide whether to take it.

**Check your work:** the tests start red and turn green as you go.

```sh
dotnet test --project samples/exercises/06-metrics/Starter.Tests
```

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
