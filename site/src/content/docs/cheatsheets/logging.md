---
title: Logging cheatsheet
description: ILogger calls, level filters, scopes, [LoggerMessage], JSON console output, OpenTelemetry export, and what to log at which level, on one page.
prev: false
next: false
learn:
  - title: Logging in .NET
    url: https://learn.microsoft.com/dotnet/core/extensions/logging/overview
  - title: Compile-time logging source generation
    url: https://learn.microsoft.com/dotnet/core/extensions/logging/source-generation
  - title: Console log formatting
    url: https://learn.microsoft.com/dotnet/core/extensions/logging/console-log-formatter
  - title: .NET observability with OpenTelemetry
    url: https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel
---

Back to [Logging](/observability/logging/).

## Calls

```csharp
// A message template: each {Placeholder} becomes a structured property, matched by position.
logger.LogInformation("Order {OrderId} placed with {ItemCount} items", orderId, itemCount);

// The exception goes first, never into the message.
logger.LogError(ex, "Payment for order {OrderId} failed", orderId);

// Don't interpolate: the result is one opaque string with no properties.
logger.LogInformation($"Order {orderId} placed");                 // ✗

// Guard expensive arguments.
if (logger.IsEnabled(LogLevel.Debug))
{
    logger.LogDebug("Basket contents: {Basket}", DescribeBasket(basket));
}
```

```csharp title="Program.cs"
// Inject ILogger<T>: T sets the category, normally the class doing the logging.
app.MapPost("/orders", (PlaceOrderRequest order, ILogger<Program> logger) => { /* ... */ });

public sealed class OrderService(ILogger<OrderService> logger) { /* ... */ }
```

## Levels

| Level            | Use for                                                 | Example                      |
| ---------------- | ------------------------------------------------------- | ---------------------------- |
| `Trace`, `Debug` | developer detail; off in production                     | the basket's contents        |
| `Information`    | a business-meaningful event, about one per unit of work | order placed, job finished   |
| `Warning`        | something unexpected was handled and the app carried on | payment declined, a retry    |
| `Error`          | the unit of work failed and someone may need to act     | payment gateway unreachable  |
| `Critical`       | the process or a dependency is unusable                 | database down, out of memory |

Log a failure **once**, where it's handled. Catch-log-rethrow writes the same error twice.

## Never log

- secrets, tokens, passwords, API keys and connection strings;
- full request or response bodies;
- personal data: emails, names, addresses, phone numbers.

Log identifiers (`OrderId`, `CustomerId`) instead. If personal data must be logged, use
[redaction](https://learn.microsoft.com/dotnet/core/extensions/compliance).

## Level filters

```json title="appsettings.json"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Orders": "Debug"
    },
    "Console": {
      "LogLevel": { "Default": "Warning" }
    }
  }
}
```

- The longest matching category prefix wins: `Orders.Payments` gets `Debug` from `Orders`.
- A provider's section (`Console`) overrides the general one for that provider.
- From the environment: `Logging__LogLevel__Default=Debug`.

## Source-generated messages

```csharp title="OrderLog.cs"
internal static partial class OrderLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Order {OrderId} placed with {ItemCount} items")]
    public static partial void OrderPlaced(ILogger logger, string orderId, int itemCount);

    // An Exception parameter becomes the entry's exception, not a property.
    [LoggerMessage(EventId = 3, Level = LogLevel.Error,
        Message = "Payment for order {OrderId} failed")]
    public static partial void PaymentFailed(ILogger logger, string orderId, Exception exception);
}
```

```csharp
OrderLog.OrderPlaced(logger, orderId, order.Items.Count);
```

Checks the level before it formats anything, and doesn't box arguments. Analyzers CA1848, CA2254 and
CA1873 point you here from plain `LogX` calls.

## Scopes

```csharp
using (logger.BeginScope(new Dictionary<string, object> { ["OrderId"] = orderId }))
{
    logger.LogInformation("Charging payment");   // carries OrderId too
}
```

ASP.NET Core already opens a scope per request (`RequestId`, `RequestPath`). A provider shows scopes
only if it's told to (`IncludeScopes`).

## JSON console output

```csharp title="Program.cs"
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
});
```

Or in configuration, with no code: `"Logging": { "Console": { "FormatterName": "json" } }`.

## Export with OpenTelemetry

```csharp title="Program.cs"
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("orders"))
    .UseOtlpExporter();
```

```sh
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 dotnet run
```

Packages: `OpenTelemetry.Extensions.Hosting` and `OpenTelemetry.Exporter.OpenTelemetryProtocol`.
Logs written inside a span carry its trace and span IDs, with no extra code.

## Test

```csharp
// Package: Microsoft.Extensions.Diagnostics.Testing
builder.ConfigureLogging(logging => logging.AddFakeLogging());      // in a WebApplicationFactory

var entries = factory.Services.GetFakeLogCollector().GetSnapshot();
var placed = entries.Single(r =>                                    // pick by property: the framework
    r.Level == LogLevel.Information &&                              // logs Information entries too
    r.StructuredState?.Any(p => p is { Key: "OrderId", Value: "ord_7f3a" }) == true);
Assert.Equal("3", placed.StructuredState!.Single(p => p.Key == "ItemCount").Value);
```
