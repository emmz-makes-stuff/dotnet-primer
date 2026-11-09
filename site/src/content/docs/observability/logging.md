---
title: "Logging"
description: "What logs, traces and metrics are each for, one OpenTelemetry setup for all three, and structured logging with ILogger: what to log, at which level, and how to keep it cheap and safe."
learn:
  - title: "Logging in .NET"
    url: https://learn.microsoft.com/dotnet/core/extensions/logging/overview
  - title: "Logging in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/fundamentals/logging/
  - title: "Compile-time logging source generation"
    url: https://learn.microsoft.com/dotnet/core/extensions/logging/source-generation
  - title: ".NET observability with OpenTelemetry"
    url: https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel
---

By the end of this page you'll know which of logs, traces and metrics answers which question, have one OpenTelemetry setup that sends all three to a dashboard on your machine, and be able to write logs that are worth reading: structured, at the right level, logged once, cheap on hot paths, and free of secrets and personal data.

:::tip[Cheatsheet]
Keep the [Logging cheatsheet](/cheatsheets/logging/) open while you work.
:::

## Three signals, three questions

Observability in .NET means three kinds of telemetry. Each answers a different question, so the first decision is which one you need:

| Signal      | Answers                                 | Example from an orders service                                          |
| ----------- | --------------------------------------- | ----------------------------------------------------------------------- |
| **Logs**    | What happened to _this_ request or job? | "Payment for order ord_7f3a was declined: vouchers are not accepted"    |
| **Traces**  | Where did the time go, across services? | `POST /orders` took 1.2 s, and 1.1 s of it was the payment gateway call |
| **Metrics** | How often, and how much, over time?     | orders placed per minute, and the 95th-percentile request duration      |

A rough guide: if you'd want to read it about one specific event, log it. If you want to see how one operation broke down into steps, trace it. If you'd want it on a graph or an alert, count or measure it. They work together: a metric tells you something is wrong, a trace tells you where, and the logs in that trace tell you what happened.

This page covers logs, and sets up all three. [Tracing](/observability/tracing/) and [Metrics](/observability/metrics/) build on the same setup.

## One setup for all three

.NET writes all three signals through its own APIs: `ILogger`, `System.Diagnostics.Activity` and `System.Diagnostics.Metrics`. [OpenTelemetry](https://opentelemetry.io/docs/languages/dotnet/) collects them and exports them over **OTLP**, the OpenTelemetry protocol, which every observability backend accepts. Your code depends on the .NET APIs, never on a vendor.

Add the packages (at the time of writing, 1.20.1 for the first two and 1.20.2 for the instrumentation):

```sh
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
```

Then register OpenTelemetry once, in `Program.cs`:

```csharp title="Program.cs"
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Logs: send ILogger entries to OpenTelemetry as well as the console.
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("orders"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()     // a span for each incoming request
        .AddHttpClientInstrumentation())    // and for each outgoing HttpClient call
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()     // http.server.request.duration and friends
        .AddHttpClientInstrumentation()
        .AddMeter("System.Runtime"))        // GC, thread pool and memory, built into .NET 9+
    .UseOtlpExporter();                     // one exporter for logs, traces and metrics

var app = builder.Build();
```

- **`AddService("orders")`** names the service, which is how the dashboard groups its telemetry. To choose the name per environment instead, leave `ConfigureResource` out and set the `OTEL_SERVICE_NAME` environment variable.
- **`UseOtlpExporter()`** reads the standard `OTEL_EXPORTER_OTLP_*` environment variables. With none set, it sends OTLP over gRPC to `http://localhost:4317`, which is where the dashboard below listens. In production, you change the variables, not the code (see [In production](#in-production)).

### See it locally

The standalone [Aspire dashboard](https://aspire.dev/dashboard/standalone/) is a single container that receives OTLP and shows logs, traces and metrics. It works with any app, not only Aspire ones. Start it with Docker:

```sh
docker run --rm -d --name aspire-dashboard \
  -p 18888:18888 -p 4317:18889 -p 4318:18890 \
  mcr.microsoft.com/dotnet/aspire-dashboard:13.6
```

Port `18888` is the UI. `4317` and `4318` are the standard OTLP gRPC and HTTP ports, mapped to the dashboard's own.

The dashboard asks for a login token. It prints a ready-made login link in its log:

```sh
docker logs aspire-dashboard
# ... Login to the dashboard at http://localhost:18888/login?t=<token>
```

On your own machine only, you can skip the token by adding `-e ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true` to the `docker run` command. That leaves the UI and its API open to anyone who can reach the port.

Run your app with `dotnet run` and send it a request. Within a few seconds, the dashboard shows the request's logs under **Structured**, its trace under **Traces**, and its metrics under **Metrics**. Stop the dashboard with `docker stop aspire-dashboard`. It keeps everything in memory, so stopping it clears the data.

## What to log

A log entry is worth writing if someone reading it later, usually during an incident, learns something they couldn't get any other way. In practice:

- **Business events**: an order placed, a payment declined, a job finished, with the identifiers that let you find the details.
- **Decisions the code made**: it fell back to a cache, it retried, it skipped a record, and why.
- **Failures**, with the exception, once (see [Log a failure once](#log-a-failure-once-where-its-handled)).

Don't log what other signals already cover. Every request already has a span with its path, status and duration, so a "request started" and "request finished" pair adds nothing. Don't log what you'd only count: "cache hit" a thousand times a second belongs in a metric.

## Choosing a level

The level says how much someone should care. Pick it by asking what happened to the unit of work, the request or job the code is doing:

| Level            | Criterion                                                                    | Example                                            |
| ---------------- | ---------------------------------------------------------------------------- | -------------------------------------------------- |
| `Trace`, `Debug` | Detail for a developer debugging this code. Off in production.               | the basket's contents before pricing               |
| `Information`    | A business-meaningful event. Roughly one per unit of work.                   | "Order ord_7f3a placed with 3 items"               |
| `Warning`        | Something unexpected happened, the code handled it, and the work carried on. | a declined payment; a retry that succeeded         |
| `Error`          | The unit of work failed, and someone may need to act.                        | the payment gateway timed out, so the order failed |
| `Critical`       | The process or a dependency it can't run without is unusable.                | the database is unreachable at startup             |

The common mistake is logging expected outcomes as errors. A declined payment is not an error: the app did exactly what it should. If it's logged at `Error`, it fires alerts and buries the real failures. Keep `Error` for work that failed.

Teams differ on `Debug` in production. Some leave it off and turn it on per category while investigating (see [Filtering by level](#filtering-by-level)). Others keep it on and filter in the backend, and pay for the volume. Either is reasonable. Choose deliberately.

## Never log secrets or personal data

Logs are copied to more places, kept for longer, and read by more people than your database. Never log:

- secrets: passwords, tokens, API keys and connection strings;
- full request or response bodies, which contain all of the above sooner or later;
- personal data: email addresses, names, postal addresses and phone numbers.

Log **identifiers** instead. The order id is enough to look up the customer when you need to, and it's safe to keep in a log store:

```csharp
// Don't: personal data in the log.
logger.LogInformation("Order {OrderId} placed by {Email}", orderId, order.CustomerEmail);

// Do: an identifier you can look up.
logger.LogInformation("Order {OrderId} placed", orderId);
```

When some data really must appear, [`Microsoft.Extensions.Compliance.Redaction`](https://learn.microsoft.com/dotnet/core/extensions/compliance) redacts or hashes classified values as they're logged.

## Log a failure once, where it's handled

A common pattern logs an exception and rethrows it:

```csharp
// Don't: log and rethrow.
try
{
    payment = gateway.Charge(orderId, order);
}
catch (PaymentGatewayException ex)
{
    logger.LogError(ex, "Payment failed");
    throw;
}
```

Every layer that does this logs the same failure again, so one timeout turns into three or four errors, each with its own stack trace. Whoever is on call can't tell whether one thing went wrong or four.

Instead, log where the failure is **handled**: where the code decides what happens next. If this code can handle it, by returning a `502`, log there and don't rethrow. If it can't, don't catch it at all: let it reach the handler that does. An unhandled exception in ASP.NET Core reaches `UseExceptionHandler`, which logs it once, at `Error` (see [Exception handling](/aspnet-core/middleware/#exception-handling-with-problem-details)). To add context on the way up, wrap the exception in one with a better message, and log only where it ends.

## The logging API

### `ILogger<T>`

Inject `ILogger<T>`, where `T` is the class doing the logging. `T` sets the entry's **category**, its full type name, which is what you filter on:

```csharp
public sealed class OrderService(ILogger<OrderService> logger)
{
    public void Ship(string orderId) =>
        logger.LogInformation("Order {OrderId} shipped", orderId);
}
```

In a minimal API handler, take it as a parameter: `(ILogger<Program> logger) => …`.

### Message templates, not interpolation

The first argument of a log call is a **message template**, not a format string. Each `{Name}` placeholder becomes a named, structured property of the entry, filled from the arguments in order:

```csharp
logger.LogInformation("Order {OrderId} placed with {ItemCount} items", orderId, items.Count);
```

The backend stores `OrderId` and `ItemCount` as fields. You can search for every entry with `OrderId = "ord_7f3a"`, or chart `ItemCount`, and the message reads the same as before.

With string interpolation, the values are baked into the text before the logger sees them:

```csharp
logger.LogInformation($"Order {orderId} placed with {items.Count} items");   // ✗
```

The entry has a message and no properties. To find one order you're back to substring search, and every entry has a different template, so you can't even group "order placed" entries together. The JSON console output below shows the difference. Analyzer CA2254 flags it, and with this site's samples' analyzer settings that's a build error.

Put the exception first, as its own argument, not in the message: `logger.LogError(ex, "Payment for order {OrderId} failed", orderId)`. The backend keeps the type, message and stack trace as separate fields.

### Filtering by level

Each provider writes only the entries at or above the minimum level for their category. Set the levels in `appsettings.json`:

```json title="appsettings.json"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Orders": "Debug"
    }
  }
}
```

- `Default` applies to every category without a more specific entry.
- The **longest matching prefix** wins: `Orders.Payments.GatewayClient` gets `Debug` from `Orders`.
- `Microsoft.AspNetCore` at `Warning` hides the framework's per-request `Information` entries, which repeat what the request spans already show.
- A provider can have its own levels, such as `"Console": { "LogLevel": { "Default": "Warning" } }`, which apply to that provider only.

`appsettings.json` is reloaded when it changes, so editing a level there takes effect without a restart. An environment variable such as `Logging__LogLevel__Orders=Debug` overrides the file, and is read at startup. See [Configuration](/aspnet-core/configuration/) for how the sources layer.

### Scopes

A **scope** attaches properties to every entry written inside it, without passing them to each call:

```csharp
using (logger.BeginScope(new Dictionary<string, object> { ["OrderId"] = orderId }))
{
    logger.LogInformation("Reserving stock");
    logger.LogInformation("Charging payment");   // both entries carry OrderId
}
```

ASP.NET Core already opens one per request, with `RequestId` and `RequestPath`. A provider includes scopes only when it's told to: `IncludeScopes = true` on the console formatters and on the OpenTelemetry logger, as in the setup above.

### Console and JSON output

The console provider's default format is human-readable text, which suits your terminal. Where logs are collected by a machine, as with a container platform's log driver, use the **JSON** formatter, which writes one JSON object per entry with every property as a field:

```csharp title="Program.cs"
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
});
```

Or select it with no code change: `"Logging": { "Console": { "FormatterName": "json" } }`.

The message-template call from earlier then writes its properties as separate fields under `State`:

```json
{
  "Timestamp": "2026-11-09T17:10:47.9571650+00:00",
  "EventId": 0,
  "LogLevel": "Information",
  "Category": "Program",
  "Message": "Order ord_7f3a placed with 3 items",
  "State": {
    "OrderId": "ord_7f3a",
    "ItemCount": 3,
    "{OriginalFormat}": "Order {OrderId} placed with {ItemCount} items"
  },
  "Scopes": [
    {
      "SpanId": "94f28ff9ce391288",
      "TraceId": "da216db0248ca8924a581497d799e736",
      "ParentId": "0000000000000000"
    },
    { "RequestId": "0HNPUGHJ18GV3:00000001", "RequestPath": "/demo" }
  ]
}
```

(The scopes are trimmed here: each also carries a `Message`, and there's a `ConnectionId` scope too.)

The interpolated version's `State` holds only `{OriginalFormat}`, and that is the finished text, `"Order ord_7f3a placed with 3 items"`. There's nothing to search or group by. `EventId` is `0` for a plain `LogInformation` call. A `[LoggerMessage]` method sets its own.

If you export with OpenTelemetry, you don't need JSON on the console for collection, and you can keep the text format for people.

### Logs and traces, joined up

When a log entry is written inside a span, such as during a request that ASP.NET Core instrumentation traces, the OpenTelemetry logger stamps it with that span's **trace ID** and **span ID**. You write no extra code. In the dashboard, a request's trace links to its log entries and each entry links back to its trace, so "what happened" and "where did the time go" are one click apart. The [Tracing](/observability/tracing/) page shows how to add spans of your own, which your log entries then join.

## Logging on hot paths

A log call costs something even when its level is turned off: the arguments are evaluated and boxed into an array, and the template is parsed. On a path that runs thousands of times a second, that shows up.

**Source-generated logging methods** remove the cost. Declare a `partial` method with `[LoggerMessage]`, and the compiler writes an implementation that checks the level first, parses the template once at build time, and doesn't box:

```csharp title="OrderLog.cs"
internal static partial class OrderLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Order {OrderId} placed with {ItemCount} items")]
    public static partial void OrderPlaced(ILogger logger, string orderId, int itemCount);

    // A declined payment is expected and handled, so it's a Warning, not an Error.
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Payment for order {OrderId} was declined: {Reason}")]
    public static partial void PaymentDeclined(ILogger logger, string orderId, string reason);

    // The gateway itself failed: the order couldn't be placed and someone may need to act, so
    // it's an Error. The exception parameter is logged as the entry's exception, not as a property.
    [LoggerMessage(EventId = 3, Level = LogLevel.Error,
        Message = "Payment for order {OrderId} failed")]
    public static partial void PaymentFailed(ILogger logger, string orderId, Exception exception);
}
```

That's `OrderLog.cs` from Exercise 04. Calling it reads well, and the level and event id live in one place:

```csharp
OrderLog.OrderPlaced(logger, orderId, order.Items.Count);
```

It's worth using everywhere, not only on hot paths: each message is defined once, with a stable event id to search on. The .NET analyzers agree. CA1848 suggests it for every plain `LogInformation(…)`-style call, and CA1873 flags arguments that are expensive to evaluate. In a project that sets `AnalysisLevel` to `latest-recommended` with warnings as errors, as this site's samples do, those are build errors.

Two more rules for hot paths:

- **Guard expensive arguments** with `if (logger.IsEnabled(LogLevel.Debug))`, so that building a description of the basket only happens when it'll be written.
- **Don't log in tight loops.** Log a summary after the loop, or count with a metric.

## In production

On your machine, the app sends OTLP straight to the dashboard. In production, it usually sends to an [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/) instead: a separate process, often a sidecar, that batches, filters and samples the telemetry, then fans it out to one or more backends. The app only knows the collector's address, `OTEL_EXPORTER_OTLP_ENDPOINT`, so you can change backends with no code change.

**On AWS**, CloudWatch accepts OTLP directly, at its own [OTLP endpoints](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch-OTLPEndpoint.html) for traces (X-Ray), logs and metrics. At the time of writing, AWS recommends putting a collector in front of them: on ECS, the [AWS Distro for OpenTelemetry (ADOT) collector](https://aws-otel.github.io/docs/setup/ecs) as a sidecar in your task; on EKS, the CloudWatch agent, through the CloudWatch Observability add-on. Either way, the app keeps exporting plain OTLP to the collector, so none of its instrumentation code changes. The [Orchestration & AWS](/deploy/orchestration-and-aws/) page covers the deployment side.

## Pitfalls

- **String interpolation in log calls.** The values become text and the properties are lost, so you can't search or group by them. Fix: a message template with named placeholders, or a `[LoggerMessage]` method.
- **Logging expected outcomes as errors.** A declined payment, a validation failure or a 404 are the app working correctly. Logged at `Error`, they bury the real failures and wake people up. Fix: `Warning` or `Information`, and keep `Error` for failed work.
- **Catch, log, rethrow.** The same failure is logged once per layer. Fix: log where it's handled, or let it reach the exception handler.
- **Personal data in messages or scopes.** An email in a scope is in every entry inside it. Fix: identifiers only, and redaction when data must appear.
- **Placeholder names that don't match the arguments.** Arguments fill placeholders by **position**, not name, so `"{OrderId} {Email}"` with the arguments swapped logs the email as `OrderId`. Fix: `[LoggerMessage]`, which matches placeholders to parameter names at compile time.
- **Nothing reaches the dashboard.** The exporter speaks gRPC to port `4317` by default. If you point it at `4318`, the HTTP port, also set `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf`. Check the dashboard container is running with `docker ps`.

## Exercise 04: logging an order endpoint

**Goal:** fix the logging of an order endpoint that works, but logs badly. Log a placed order once at `Information` with its id and item count as properties, log a declined payment once at `Warning` with the reason, and keep the customer's email address out of every log entry.

**Where:** [`samples/exercises/04-logging`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/04-logging) in the GitHub repo. Clone the repo, then work in the `Starter` project. The tests capture log entries in memory with `AddFakeLogging()`, so you don't need Docker.

**Steps:**

1. Open `Starter/OrderLog.cs`. Following the `PaymentFailed` method that's already there, add the `OrderPlaced` and `PaymentDeclined` source-generated methods, and choose `PaymentDeclined`'s level (TODO 1).
2. In `Starter/Program.cs`, replace the two `LogError` calls for a declined payment with one `OrderLog.PaymentDeclined` call (TODO 2).
3. Replace the interpolated "order placed" call with `OrderLog.OrderPlaced`, and log the order id, not the email (TODO 3). Then remove the `#pragma warning disable` and `restore` lines: with the bad calls gone, the analyzers have nothing to complain about.

**Check your work:** the tests start red and turn green as you go.

```sh
dotnet test --project samples/exercises/04-logging/Starter.Tests
```

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
