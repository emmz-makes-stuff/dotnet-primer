---
title: "Tracing"
description: "Distributed tracing with ActivitySource and OpenTelemetry: following a request across services, where spans belong, naming and attributes, recording failures, sampling, and viewing traces locally."
learn:
  - title: ".NET distributed tracing"
    url: https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing
  - title: "Add distributed tracing instrumentation"
    url: https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
  - title: ".NET observability with OpenTelemetry"
    url: https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel
---

By the end of this page you'll be able to read a trace and see where a request's time went, decide which work deserves a span of its own, add spans with useful attributes to your code, record failures on them, and choose a sampling strategy that keeps the traces you need.

:::tip[Cheatsheet]
Keep the [Tracing cheatsheet](/cheatsheets/tracing/) open while you work.
:::

Building on the setup from [Logging](/observability/logging/#one-setup-for-all-three): that page's `WithTracing(…)` call already traces incoming requests and outgoing `HttpClient` calls, and sends them to the local dashboard. This page adds your own spans to it.

## Traces, spans and context

A **trace** is the story of one request as it moves through your system. It's made of **spans**: each span is one unit of work, with a name, a start time, a duration, and **attributes** (key-value pairs) that describe it. Spans form a tree. The request your service receives is the root span, and each piece of work inside it is a child, with its own children.

In the dashboard, a trace is a **waterfall**:

```text
POST /orders                        ████████████████████████████  1.21 s
  PlaceOrder                         ███████████████████████████  1.19 s
    ChargePayment                     ██████████████████████████  1.12 s
      POST https://payments.test/…     █████████████████████████  1.11 s
```

One look says the order was slow because the payment gateway was.

A trace crosses services because each call carries its **trace context**: the trace id, and the id of the span that made the call. The standard for HTTP is [W3C Trace Context](https://www.w3.org/TR/trace-context/), a `traceparent` header:

```text
traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
```

That's the version, the 32-hex-digit trace id, the caller's 16-hex-digit span id, and the flags (`01` means sampled). `HttpClient` adds the header to outgoing requests, and ASP.NET Core reads it from incoming ones, so services that both use .NET, or any OpenTelemetry SDK, join the same trace without any code.

### The .NET names

.NET had distributed tracing before OpenTelemetry existed, so it uses its own names for the same ideas. OpenTelemetry's .NET SDK uses the .NET types directly: there's no separate tracer to learn.

| OpenTelemetry | .NET (`System.Diagnostics`) |
| ------------- | --------------------------- |
| Tracer        | `ActivitySource`            |
| Span          | `Activity`                  |
| Attribute     | tag, set with `SetTag`      |
| Span kind     | `ActivityKind`              |
| Status        | `ActivityStatusCode`        |
| Span context  | `ActivityContext`           |

`Activity.Current` is the span in progress. It flows with `async` code, so it's the right parent wherever you are in a request.

## Where a span belongs

A span is worth having when its duration on its own answers a question. Start one for:

- **an incoming request or message**, which the instrumentation does for you;
- **an outgoing call**: HTTP, a database query, a cache, a queue. `HttpClient` calls are traced for you; others need their instrumentation package or a span of yours;
- **a business step worth timing**: placing an order, pricing a basket, generating a report, especially one that makes several calls you want to see grouped.

Don't start one for every method. A span per method makes traces enormous and expensive to store, and the waterfall becomes noise. If a method is quick, does no I/O, and you'd never ask "how long did this take?", it doesn't need a span. If you want to know that something happened inside a span, without timing it, add an [event](#events-and-links) instead.

## Naming spans

A span's name is what you group, search and compare by, so it must be **low-cardinality**: the same for every instance of the same operation.

- **Do:** `PlaceOrder`, `ChargePayment`, `GET /orders/{id}`.
- **Don't:** `PlaceOrder ord_7f3a`, `GET /orders/42`, or anything with an id, a user or a timestamp in it.

Ids go in **attributes**, where a different value per span is fine. ASP.NET Core instrumentation follows the same rule: it names request spans by method and **route template**, not by the actual path.

## Choosing attributes

Attributes are what make a span searchable: "show me the slow `PlaceOrder` spans with more than 20 items".

- **Use the [semantic conventions](https://opentelemetry.io/docs/specs/semconv/) where they apply.** They're OpenTelemetry's standard names, which backends understand: `http.request.method`, `http.response.status_code`, `url.path`, `server.address`, `exception.type`. The instrumentation already sets them on its spans, so don't duplicate them.
- **For your own domain, use a dotted namespace of your own:** `order.id`, `order.item_count`, `payment.method`. Lower case, with words separated by underscores.
- **Add what you'd filter or group by:** ids, counts, categories, outcomes. High cardinality is fine in attributes. That's what they're for.
- **Leave out personal data and secrets,** for the same reasons as in logs: traces are stored and shared as widely.

At the time of writing, only some semantic conventions are stable, among them `http.*` and `exception.*`. Others, such as the database and messaging ones, may still change.

## Recording a failure

When the work in a span fails, mark the span as failed and record the exception on it:

```csharp
catch (PaymentGatewayException ex)
{
    chargePayment?.SetStatus(ActivityStatusCode.Error, ex.Message);
    chargePayment?.AddException(ex);
    // handle it, or rethrow
}
```

- **`SetStatus(ActivityStatusCode.Error, …)`** is what makes the dashboard draw the span in red, and what error-rate queries count. A span's status stays `Unset` unless you set it, even if an exception passed through.
- **`AddException(ex)`** (.NET 9 and later) adds an `exception` event with the semantic-convention attributes `exception.type`, `exception.message` and `exception.stacktrace`.

Record it **on the span where it happened**, once, as with logging. ASP.NET Core's instrumentation already marks the request span as an error when the response is a 5xx, so you don't need to mark every span on the way up. In Exercise 05, only `ChargePayment` is marked: the error belongs to the payment step, `PlaceOrder` stays `Unset`, and the `POST /orders` request span above it is already an error because of the 502.

## Events and links

An **event** is a moment inside a span: something that happened at a point in time, with its own attributes, but with no duration worth measuring.

```csharp
placeOrder?.AddEvent(new ActivityEvent("stock.reserved",
    tags: new ActivityTagsCollection { ["sku"] = "SKU-1" }));
```

Use an event rather than a child span when you'd only want to know _that_ and _when_ it happened. The exception that `AddException` records is an event too.

A **link** connects a span to spans in **other** traces, when parent and child doesn't describe the relationship. The usual case is a batch: a consumer processes 50 queued messages in one span, and each message came from a different trace. The batch span can't have 50 parents, but it can link to all 50:

```csharp
var links = messages.Select(m => new ActivityLink(m.Context));
using var batch = OrderTracing.Source.StartActivity(
    "ProcessBatch", ActivityKind.Consumer, parentContext: default, links: links);
```

## Sampling

Tracing every request in a busy service costs a lot to send and store, and most traces of a healthy service look the same. **Sampling** keeps a fraction:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1))));   // keep 10%
```

- **`TraceIdRatioBasedSampler(0.1)`** keeps 10% of new traces, chosen by trace id.
- **`ParentBasedSampler`** around it makes every service follow the caller's decision, which arrives in the `traceparent` flags. That keeps a trace **whole**: without it, each service samples independently, and you get traces with holes.

The default is `ParentBasedSampler(AlwaysOnSampler)`: keep everything. That's right on your machine, and often fine for low-traffic services.

This is **head sampling**: the decision is made when the trace starts, before anyone knows how it will go. A 10% sample drops 90% of the failing and slow traces too. Keeping all errors and slow requests needs **tail sampling**, which waits until a trace is complete. That's done outside the app, in an [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/), so sample less in the app when you have one.

## Baggage, carefully

**Baggage** is a set of key-value pairs that travels with the trace context, in a `baggage` header, to every service downstream. It's handy for values every service needs, such as a tenant id or a feature flag. But it's sent on **every** outgoing call, including calls to third-party APIs, and any service on the path can read it.

- **Never put secrets, tokens or personal data in baggage.**
- Keep it small: it's added to every request.
- Baggage isn't added to spans automatically. If you want a value on spans, set it as an attribute where you need it.

## Automatic instrumentation

The setup on the Logging page registers two instrumentation libraries:

- **`AddAspNetCoreInstrumentation()`** starts a server span for each incoming request, named by method and route (`POST /orders`), with the HTTP semantic-convention attributes. It joins the caller's trace when there's a `traceparent` header.
- **`AddHttpClientInstrumentation()`** starts a client span for each outgoing `HttpClient` request, and adds the `traceparent` header to it.

Both take options. A common one keeps health checks out of your traces:

```csharp title="Program.cs"
.AddAspNetCoreInstrumentation(options =>
    options.Filter = context => context.Request.Path != "/health")
```

Other libraries, such as database clients and message brokers, have their own instrumentation packages or publish their own `ActivitySource`s. Search NuGet for `OpenTelemetry.Instrumentation.`, and check each package's maturity: many are still prerelease.

## Custom spans

For your own spans, create one `ActivitySource` per component, once, and keep it in a static field:

```csharp title="OrderTracing.cs"
using System.Diagnostics;

namespace Orders;

/// <summary>
/// The app's source of spans. One static <see cref="ActivitySource"/> per component, named after
/// it; OpenTelemetry subscribes to it by that name (<c>.AddSource("Orders")</c>).
/// </summary>
internal static class OrderTracing
{
    public const string SourceName = "Orders";

    public static readonly ActivitySource Source = new(SourceName);
}
```

Then start spans from it with `StartActivity`, inside a `using`, so that disposing the activity ends the span. This is the order endpoint from Exercise 05:

```csharp title="Program.cs"
// A span for the business step. StartActivity makes it a child of the current span, the
// incoming request's, and returns null when nothing is listening: hence the ?. calls.
// The name is low-cardinality; the order id goes in an attribute.
using var placeOrder = OrderTracing.Source.StartActivity("PlaceOrder");
placeOrder?.SetTag("order.id", orderId);
placeOrder?.SetTag("order.item_count", order.Items.Count);

PaymentResult payment;
// A child span for the outgoing call, so its time shows separately in the waterfall.
using (var chargePayment = OrderTracing.Source.StartActivity("ChargePayment"))
{
    try
    {
        payment = payments.Charge(orderId, order);
    }
    catch (PaymentGatewayException ex)
    {
        // Mark the span that failed, and record the exception on it as an "exception" event.
        chargePayment?.SetStatus(ActivityStatusCode.Error, ex.Message);
        chargePayment?.AddException(ex);
        return Results.Json(new PlaceOrderResponse(orderId, "failed", "the payment gateway is unavailable"), statusCode: StatusCodes.Status502BadGateway);
    }
}
```

- **`StartActivity` returns `null`** when nothing is listening to the source, or when the sampler drops the trace. That's what makes tracing nearly free when it's off. So every call on the activity uses [`?.`](/csharp/null-safety/#the-null-operators--c-6-and--c-8).
- **The parent is `Activity.Current`**, here the request's span, so you don't pass it in.
- **`ActivityKind`** defaults to `Internal`. Pass `ActivityKind.Client` for a span around an outgoing call that isn't instrumented already, so backends draw it as a call to another service.
- To add attributes to a span you didn't start, such as the request's, use `Activity.Current?.SetTag(…)`.

### Registering the source

OpenTelemetry only listens to the sources you name. Add yours to the setup:

```csharp title="Program.cs"
.WithTracing(tracing => tracing
    .AddSource(OrderTracing.SourceName)   // or "Orders.*" for every source under Orders
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation())
```

Without `AddSource`, nothing listens to `Orders`, so `StartActivity` returns `null`, and your spans silently don't exist.

## Viewing a trace

With the [dashboard running](/observability/logging/#see-it-locally), send the app a request, then open **Traces** at `http://localhost:18888`. Each row is a trace, named after its root span, with its duration and span count. Select one to see the waterfall:

- each bar is a span, indented under its parent, and its length is its duration;
- a red bar is a span with error status;
- select a span to see its attributes and events, including any recorded exception;
- the trace's log entries are linked from it, because [logs carry the trace id](/observability/logging/#logs-and-traces-joined-up).

If a span you expect is missing, check the [Pitfalls](#pitfalls).

## In production

On your machine, the app sends OTLP straight to the dashboard. In production, it usually sends to an [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/) instead: a separate process, often a sidecar, that batches the telemetry, applies tail sampling, filters it, and fans it out to one or more backends. The app only knows the collector's address, `OTEL_EXPORTER_OTLP_ENDPOINT`, so you can change backends with no code change.

**On AWS**, CloudWatch accepts OTLP directly, at its own [OTLP endpoints](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch-OTLPEndpoint.html) for traces (X-Ray), logs and metrics. At the time of writing, AWS recommends putting a collector in front of them: on ECS, the [AWS Distro for OpenTelemetry (ADOT) collector](https://aws-otel.github.io/docs/setup/ecs) as a sidecar in your task; on EKS, the CloudWatch agent, through the CloudWatch Observability add-on. Either way, the app keeps exporting plain OTLP to the collector, so none of its instrumentation code changes. The [Orchestration & AWS](/deploy/orchestration-and-aws/) page covers running the app itself on ECS and EKS. For the collector, follow the links above.

## Pitfalls

- **`StartActivity` returns `null`, and your spans never appear.** Nothing is listening to the source: it isn't registered with `AddSource`, or the name doesn't match exactly. Fix: register it, ideally from the same constant (`AddSource(OrderTracing.SourceName)`), and keep the `?.` calls, since `null` is also normal when a trace isn't sampled.
- **A span that never ends.** An activity that isn't disposed is never stopped, so it's never exported, and the spans after it may be parented on it. Fix: `using var` or a `using` block for every `StartActivity`.
- **A new `ActivitySource` per request.** Sources are meant to live as long as the app, and listeners attach to each new one. Fix: one static source per component.
- **High-cardinality span names.** An id in the name gives every span a unique name, so you can't group or compare them. Fix: a fixed name, and the id as an attribute.
- **Failures that look like successes.** A caught exception leaves the span's status `Unset`, so the dashboard shows it as fine. Fix: `SetStatus(ActivityStatusCode.Error, …)` and `AddException(ex)` where the failure happens.
- **Traces with holes.** Each service samples on its own, so some keep a trace that others dropped. Fix: `ParentBasedSampler` everywhere, and tail sampling in a collector.

## Exercise 05: tracing an order endpoint

**Goal:** add tracing to an order endpoint that works but has none. Give placing the order its own span, a child of the request's, with the order id and item count as attributes, put the payment step in a child span of its own, and mark that span as failed, with the exception recorded, when the payment gateway throws.

**Where:** [`samples/exercises/05-tracing`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/05-tracing) in the GitHub repo. Clone the repo, then work in the `Starter` project. The tests record spans in memory with an `ActivityListener`, so you don't need Docker. Each test sends its own `traceparent` header, so it can pick out its request's trace, which is propagation at work.

**Steps:**

1. Open `Starter/OrderTracing.cs`, and add the static `ActivitySource` (TODO 1).
2. In `Starter/Program.cs`, start the `PlaceOrder` span and give it the `order.id` and `order.item_count` attributes (TODO 2).
3. Put the payment step in a `ChargePayment` span (TODO 3).
4. In the `catch` block, set the `ChargePayment` span's status to error, and record the exception on it (TODO 4).

**Check your work:** the tests start red and turn green as you go.

```sh
dotnet test --project samples/exercises/05-tracing/Starter.Tests
```

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
