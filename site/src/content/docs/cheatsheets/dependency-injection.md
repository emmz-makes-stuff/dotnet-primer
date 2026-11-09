---
title: Dependency Injection cheatsheet
description: Lifetimes, registration one-liners, injection sites and scopes, on one page.
prev: false
next: false
learn:
  - title: Dependency injection in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection
  - title: Service lifetimes
    url: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/service-lifetimes
  - title: Dependency injection guidelines
    url: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/guidelines
---

Back to [Dependency Injection](/aspnet-core/dependency-injection/).

## Lifetimes

| Lifetime  | One instance per… | Use for                                                  | May depend on      |
| --------- | ----------------- | -------------------------------------------------------- | ------------------ |
| Singleton | app               | caches, clients, `TimeProvider`. **Must be thread-safe** | singletons         |
| Scoped    | request (scope)   | `DbContext`, per-request state, most app services        | scoped, singletons |
| Transient | resolution        | cheap, stateless helpers                                 | anything           |

Scope validation (captive dependencies, scoped from root) runs in `Development` only.

## Register

```csharp title="Program.cs"
builder.Services.AddSingleton<VisitCounter>();                    // concrete type
builder.Services.AddScoped<IOrderStore, SqlOrderStore>();          // interface → implementation
builder.Services.AddTransient<IPriceFormatter, PriceFormatter>();
builder.Services.AddSingleton(TimeProvider.System);                // existing instance (singleton)
builder.Services.AddSingleton<IPaymentGateway>(sp =>               // factory
    new StripeGateway(sp.GetRequiredService<IConfiguration>()["Payments:ApiKey"] ?? ""));
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));  // open generic
```

## Several implementations

```csharp title="Program.cs"
using Microsoft.Extensions.DependencyInjection.Extensions;

builder.Services.AddSingleton<INotifier, EmailNotifier>();
builder.Services.AddSingleton<INotifier, SmsNotifier>();
// INotifier              → SmsNotifier (last registration wins)
// IEnumerable<INotifier> → EmailNotifier, SmsNotifier

builder.Services.TryAddSingleton<IClock, SystemClock>();         // only if IClock isn't registered
builder.Services.TryAddEnumerable(                                // only if this pair isn't there
    ServiceDescriptor.Singleton<INotifier, EmailNotifier>());
```

## Keyed services

```csharp title="Program.cs"
builder.Services.AddKeyedSingleton<IFileStore, S3FileStore>("images");
builder.Services.AddKeyedSingleton<IFileStore, LocalFileStore>("reports");

app.MapGet("/images/{name}", (string name, [FromKeyedServices("images")] IFileStore images) => …);

public sealed class ReportService([FromKeyedServices("reports")] IFileStore reports) { … }
```

## Inject

```csharp
// Constructor (primary constructor)
public sealed class OrderService(IOrderStore store, TimeProvider time, ILogger<OrderService> logger) { … }

// Minimal API handler parameter
app.MapPost("/orders", (NewOrder input, OrderService orders) => …);

// Ambiguous? Say it's a service
app.MapPost("/orders", (NewOrder input, [FromServices] OrderService orders) => …);
```

## Scoped work outside a request

```csharp
public sealed class Worker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orders = scope.ServiceProvider.GetRequiredService<OrderService>();
        // … use orders; scoped services are disposed with the scope
    }
}

builder.Services.AddHostedService<Worker>();
```

At startup, in `Program.cs`:

```csharp title="Program.cs"
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    // migrate, seed, …
}
```

## Errors you'll meet

| Message                                                                    | Cause                                                             | Fix                                                        |
| -------------------------------------------------------------------------- | ----------------------------------------------------------------- | ---------------------------------------------------------- |
| `Cannot consume scoped service 'X' from singleton 'Y'`                     | captive dependency                                                | make `Y` scoped, or create a scope inside `Y`              |
| `Cannot resolve scoped service 'X' from root provider`                     | resolved from `app.Services`                                      | `CreateScope()` first                                      |
| `Unable to resolve service for type 'X' while attempting to activate 'Y'`  | `X` not registered                                                | register `X`                                               |
| `Body was inferred but the method does not allow inferred body parameters` | handler parameter type not registered                             | register it, or add `[FromServices]` to see the real error |
| `No service for type 'X' has been registered`                              | `[FromServices]` or `GetRequiredService` for an unregistered type | register `X`                                               |
| `The service collection cannot be modified because it is read-only`        | registering after `Build()`                                       | register before `Build()`                                  |
