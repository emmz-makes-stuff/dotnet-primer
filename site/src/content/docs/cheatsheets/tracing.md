---
title: Tracing cheatsheet
description: ActivitySource and spans, attributes, status and exceptions, events and links, registration, instrumentation, sampling and traceparent, on one page.
prev: false
next: false
learn:
  - title: .NET distributed tracing
    url: https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing
  - title: Add distributed tracing instrumentation
    url: https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
  - title: .NET observability with OpenTelemetry
    url: https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel
---

Back to [Tracing](/observability/tracing/).

## Names

| OpenTelemetry | .NET                 |
| ------------- | -------------------- |
| Tracer        | `ActivitySource`     |
| Span          | `Activity`           |
| Attribute     | tag (`SetTag`)       |
| Span kind     | `ActivityKind`       |
| Status        | `ActivityStatusCode` |
| Span context  | `ActivityContext`    |

## Start a span

```csharp title="OrderTracing.cs"
using System.Diagnostics;

internal static class OrderTracing
{
    public static readonly ActivitySource Source = new("Orders");   // one per component, kept forever
}
```

```csharp
// A child of Activity.Current. Null when nothing listens: always use ?.
using var activity = OrderTracing.Source.StartActivity("PlaceOrder");
activity?.SetTag("order.id", orderId);
activity?.SetTag("order.item_count", order.Items.Count);

// Outgoing call or message: say so with the kind.
using var call = OrderTracing.Source.StartActivity("ChargePayment", ActivityKind.Client);

// Add to the span someone else started (such as the request's).
Activity.Current?.SetTag("customer.tier", "gold");

// Skip work when the span won't be recorded.
if (activity is { IsAllDataRequested: true })
{
    activity.SetTag("order.summary", Describe(order));
}
```

Names are low-cardinality: `PlaceOrder`, never `PlaceOrder ord_7f3a`. Ids go in tags.

## Failures

```csharp
catch (PaymentGatewayException ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.AddException(ex);          // .NET 9+: an "exception" event with type, message, stack
    throw;                               // or handle it
}
```

## Events and links

```csharp
// Something that happened at a point in time, inside the span.
activity?.AddEvent(new ActivityEvent("stock.reserved",
    tags: new ActivityTagsCollection { ["sku"] = "SKU-1" }));

// A span related to other traces, not parented on them (a batch of queued messages).
var links = messages.Select(m => new ActivityLink(m.Context));
using var batch = OrderTracing.Source.StartActivity(
    "ProcessBatch", ActivityKind.Consumer, parentContext: default, links: links);
```

## Register sources and instrumentation

```csharp title="Program.cs"
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Orders")                        // your own sources, by name ("Orders.*" works)
        .AddAspNetCoreInstrumentation(options =>
            options.Filter = context => context.Request.Path != "/health")
        .AddHttpClientInstrumentation())
    .UseOtlpExporter();
```

Packages: `OpenTelemetry.Instrumentation.AspNetCore` and `OpenTelemetry.Instrumentation.Http`, plus
the [Logging page's setup](/observability/logging/#one-setup-for-all-three).

## Sampling

```csharp
tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)));   // keep 10%
```

Parent-based: follow the caller's decision, so a trace is kept or dropped whole. The decision is made
at the start, before anyone knows the trace will fail.

## `traceparent`

```text
traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
             │  │                                │                └ flags (01 = sampled)
             │  │                                └ parent span id (16 hex)
             │  └ trace id (32 hex)
             └ version
```

`HttpClient` sends it, and ASP.NET Core reads it, with no code. Read one yourself with
`ActivityContext.TryParse(traceparent, tracestate, out var context)`.

## Baggage

Key-value pairs sent to **every** downstream service, third parties included, in the `baggage`
header. Never put secrets or personal data in it.

## See it locally

```sh
docker run --rm -d --name aspire-dashboard \
  -p 18888:18888 -p 4317:18889 -p 4318:18890 \
  mcr.microsoft.com/dotnet/aspire-dashboard:13.6
docker logs aspire-dashboard        # the login link, with its token
```

UI on `http://localhost:18888`, **Traces** tab.
