---
title: "Dependency Injection"
description: "The built-in service container: registering services, choosing singleton, scoped or transient, constructor and handler injection, keyed services, and avoiding captive dependencies."
learn:
  - title: "Dependency injection in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection
  - title: ".NET dependency injection"
    url: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/overview
  - title: "Service lifetimes"
    url: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/service-lifetimes
  - title: "Dependency injection guidelines"
    url: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/guidelines
---

By the end of this page you'll be able to register your own services with ASP.NET Core's built-in container, choose singleton, scoped or transient for each one with a reason, and have them injected into classes and endpoint handlers. You'll recognise a captive dependency before it reaches production, and know how to use scoped services from background work, where there's no request.

:::tip[Cheatsheet]
Keep the [Dependency Injection cheatsheet](/cheatsheets/dependency-injection/) open while you work.
:::

## The container builds your objects

Classes declare what they need as constructor parameters, and the **service container** creates them, supplying those dependencies. You register types on `builder.Services` before `Build()`. From then on, nothing in your app calls `new` on a service:

```csharp title="Program.cs"
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

app.MapPost("/orders", (NewOrder input, OrderService orders) =>
{
    var order = orders.Place(input.Product, input.Quantity);
    return TypedResults.Created($"/orders/{order.Id}", order);
});

app.MapGet("/orders/{id:guid}", Results<Ok<Order>, NotFound> (Guid id, IOrderStore store) =>
    store.Find(id) is { } order ? TypedResults.Ok(order) : TypedResults.NotFound());

app.Run();

public sealed record Order(Guid Id, string Product, int Quantity, DateTimeOffset PlacedAt);

public sealed record NewOrder(string Product, int Quantity);

public interface IOrderStore
{
    void Add(Order order);
    Order? Find(Guid id);
}

public sealed class InMemoryOrderStore : IOrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public void Add(Order order) => _orders[order.Id] = order;

    public Order? Find(Guid id) => _orders.GetValueOrDefault(id);
}

public sealed class OrderService(IOrderStore store, TimeProvider time)
{
    public Order Place(string product, int quantity)
    {
        var order = new Order(Guid.NewGuid(), product, quantity, time.GetUtcNow());
        store.Add(order);
        return order;
    }
}
```

- `OrderService` asks for an `IOrderStore` and a `TimeProvider`. It doesn't know or care which implementations it gets.
- The handlers ask for `OrderService` and `IOrderStore` as parameters, and ASP.NET Core resolves them from the container for each request.
- Swapping `InMemoryOrderStore` for a database-backed store is a one-line change to the registration. In tests, you can replace it the same way.

Everything the framework provides is registered in the same container: `ILogger<T>`, `IConfiguration`, `IOptions<T>` (see [Configuration](/aspnet-core/configuration/)), `IHttpClientFactory`, `TimeProvider`, and so on. One container, built in, with no third-party library needed.

## Lifetimes

Every registration has a **lifetime**. It decides how long an instance lives, and so who shares it:

| Lifetime      | Register with  | One instance per…                                                       | Typical use                                                                       |
| ------------- | -------------- | ----------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| **Singleton** | `AddSingleton` | app: created on first use, disposed at shutdown                         | caches, clients, configuration-like services, `TimeProvider`. Must be thread-safe |
| **Scoped**    | `AddScoped`    | request (more precisely, per scope; ASP.NET Core opens one per request) | per-request state, units of work, EF Core's `DbContext`                           |
| **Transient** | `AddTransient` | resolution: a new one every time it's asked for                         | lightweight, stateless helpers                                                    |

How to choose:

- **Does it hold state that every request should share?** It's a singleton, and it must be safe to use from many threads at once.
- **Does it hold state for one request, or depend on something scoped?** Make it scoped. Most application services (order services, repositories) are scoped, because they use a scoped `DbContext`.
- **Is it cheap and stateless?** Transient is fine, but so is scoped. When unsure, scoped is the safe default for application services.

The container disposes what it creates. A scoped `IDisposable` is disposed when the request ends, and a singleton at shutdown. Don't dispose injected services yourself.

## Constructor and primary-constructor injection

A class gets its dependencies through its constructor. With C# 12 and later, a **primary constructor** says the same thing without the field boilerplate. These two classes are equivalent:

```csharp title="InvoiceService.cs"
public sealed class InvoiceService
{
    private readonly TimeProvider _time;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(TimeProvider time, ILogger<InvoiceService> logger)
    {
        _time = time;
        _logger = logger;
    }

    public DateTimeOffset Issue()
    {
        var now = _time.GetUtcNow();
        _logger.LogInformation("Invoice issued at {IssuedAt}", now);
        return now;
    }
}

public sealed class ReceiptService(TimeProvider time, ILogger<ReceiptService> logger)
{
    public DateTimeOffset Issue()
    {
        var now = time.GetUtcNow();
        logger.LogInformation("Receipt issued at {IssuedAt}", now);
        return now;
    }
}
```

The primary-constructor parameters (`time`, `logger`) are in scope for the whole class. Unlike `readonly` fields, they can be reassigned, so don't. If a class has more than one public constructor, the container uses the one whose parameters it can satisfy the most of. Keep to one constructor and you never have to think about it.

## Injecting into minimal API handlers

A handler parameter whose type is registered in the container is resolved from it, as `OrderService orders` is above. Services are resolved per request, so a scoped service in a handler is that request's instance.

When ASP.NET Core can't tell whether a parameter is a service or the request body, mark it with `[FromServices]`. If a parameter type isn't registered at all, ASP.NET Core assumes it's the JSON body. On a `GET` that fails with "Body was inferred but the method does not allow inferred body parameters". That error almost always means a missing registration. Adding `[FromServices]` turns it into the plainer "No service for type '…' has been registered."

## Registration patterns

```csharp title="Program.cs"
// Interface to implementation
builder.Services.AddScoped<IOrderStore, InMemoryOrderStore>();

// Factory: you decide how to build it, with access to other services
builder.Services.AddSingleton<IPaymentGateway>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new StripeGateway(config["Payments:ApiKey"] ?? "");
});

// An instance you already have
builder.Services.AddSingleton(TimeProvider.System);

// Several implementations of one interface
builder.Services.AddSingleton<INotifier, EmailNotifier>();
builder.Services.AddSingleton<INotifier, SmsNotifier>();

// Register only if nothing is registered yet (library defaults)
builder.Services.TryAddSingleton<IClock, SystemClock>();

// Add to a collection only if that implementation isn't there yet
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<INotifier, EmailNotifier>());

// Open generics: one registration for every T
builder.Services.AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>));

// Keyed services: several of one type, chosen by key
builder.Services.AddKeyedSingleton<IFileStore, LocalFileStore>("images");
builder.Services.AddKeyedSingleton<IFileStore, LocalFileStore>("reports");
```

- **Interface to implementation** is the everyday pattern. Consumers depend on `IOrderStore`, and the registration picks the class.
- **A factory** (`sp => …`) lets you decide how to construct the object, using other services from `sp`. Use it for types the container can't build on its own, such as one that needs a configuration value in its constructor.
- **An instance** registers an object you already created. It's always a singleton.
- **Several implementations** of one interface are all kept. Inject `IEnumerable<INotifier>` to get every one, in registration order. Inject plain `INotifier` and you get the **last** one registered.
- **`TryAdd…`** registers only if the service type has no registration yet. Libraries use it to provide defaults that the app can override. It lives in `Microsoft.Extensions.DependencyInjection.Extensions`.
- **`TryAddEnumerable`** adds an implementation to a collection only if that exact implementation isn't already in it.
- **Open generics** register `IRepository<>` once, and serve `IRepository<Order>`, `IRepository<Customer>` and every other closed type.
- **Keyed services** register several implementations of one type under different keys. Ask for one with `[FromKeyedServices("key")]`.

Using them from handlers:

```csharp title="Program.cs"
app.MapPost("/notify", async (string message, IEnumerable<INotifier> notifiers) =>
{
    foreach (var notifier in notifiers)
    {
        await notifier.SendAsync(message);
    }

    return TypedResults.NoContent();
});

app.MapGet("/images/{name}", Results<Ok<string>, NotFound> (string name, [FromKeyedServices("images")] IFileStore images) =>
    images.Exists(name) ? TypedResults.Ok(name) : TypedResults.NotFound());
```

`[FromKeyedServices]` works on constructor parameters too:

```csharp title="ReportService.cs"
public sealed class ReportService([FromKeyedServices("reports")] IFileStore reports)
```

## Captive dependencies

A **captive dependency** is a longer-lived service holding on to a shorter-lived one. The classic case is a singleton that takes a scoped service in its constructor:

```csharp title="Program.cs"
builder.Services.AddScoped<ShoppingCart>();
builder.Services.AddSingleton<PriceCache>();   // PriceCache(ShoppingCart cart)
```

The singleton is created once, so it captures the first request's `ShoppingCart` and keeps it forever. Every later request then sees another user's cart, and a scoped `DbContext` captured this way gets used from many threads at once, which it doesn't support. It's a data-leak bug and a concurrency bug.

In the `Development` environment, ASP.NET Core validates scopes when the app starts, and refuses to build:

```text
AggregateException: Some services are not able to be constructed (Error while validating the
service descriptor 'ServiceType: PriceCache Lifetime: Singleton ImplementationType: PriceCache':
Cannot consume scoped service 'ShoppingCart' from singleton 'PriceCache'.)
```

In `Production`, that validation is **off**. The same app starts without complaint and the bug ships. So run locally in `Development`, and treat that error as a design problem. Make the consumer scoped too, or have the singleton create a scope when it needs the scoped service (next section).

The rule: **a service may only depend on services that live at least as long as it does.** Singleton → singletons only. Scoped → scoped or singleton. Transient → anything, but a transient used by a singleton becomes effectively a singleton. `IOptionsSnapshot<T>` is scoped, so it's caught by the same check.

## Scopes outside a request

Background work has no request, and so no scope. Resolving a scoped service from the app's root provider throws `Cannot resolve scoped service 'ShoppingCart' from root provider.` in `Development`. Instead, inject `IServiceScopeFactory` and create a scope for each unit of work:

```csharp title="Program.cs"
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<OrderArchiver>();
builder.Services.AddHostedService<NightlyArchive>();

var app = builder.Build();
app.Run();

public sealed class OrderArchiver
{
    public Task ArchiveOldOrdersAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class NightlyArchive(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        do
        {
            // One scope per unit of work, like a request: scoped services
            // (a DbContext, say) live until the scope is disposed.
            await using var scope = scopeFactory.CreateAsyncScope();
            var archiver = scope.ServiceProvider.GetRequiredService<OrderArchiver>();
            await archiver.ArchiveOldOrdersAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
```

`IServiceScopeFactory` is a singleton, so it's safe to inject into a `BackgroundService` (itself a singleton). Everything resolved from `scope.ServiceProvider` lives until the scope is disposed at the end of the iteration. That's the same lifecycle a request gives a scoped service.

## Pitfalls

- **Captive dependencies.** A singleton depends on a scoped service, so it shares one request's state with everyone. Fix: follow the lifetime rule above, and develop in `Development` so validation catches it.
- **Trusting `Production` to catch lifetime bugs.** Scope validation is off there by default. Fix: run and test in `Development`. `WebApplicationFactory` tests run there by default.
- **Resolving scoped services from `app.Services`.** That's the root provider, which has no scope. Fix: `using var scope = app.Services.CreateScope();` and resolve from `scope.ServiceProvider`.
- **A service locator in disguise.** Injecting `IServiceProvider` and calling `GetService` everywhere hides dependencies and defeats validation. Fix: ask for what you need in the constructor. Keep `IServiceProvider` and `IServiceScopeFactory` for infrastructure code like the background service above.
- **A singleton that isn't thread-safe.** A `List<T>` or `Dictionary` in a singleton is shared by every concurrent request. Fix: use concurrent collections or locking, or make it scoped.
- **Registering twice and getting the wrong one.** A second `AddSingleton<INotifier, …>` doesn't replace the first. Both are kept, and a single resolve silently gets the last. Fix: `TryAdd…` for defaults, and `IEnumerable<T>` when you mean "all of them".
- **Disposing injected services.** The container owns them, and will dispose them again (or another consumer is still using them). Fix: dispose only what you created with `new`.
- **Work in constructors.** The container builds objects on demand, often on a request's critical path. Fix: keep constructors to assigning dependencies. Do I/O in methods, or in a hosted service at startup.

## Exercise 02: choose the right lifetimes

**Goal:** register three services so their lifetimes behave correctly. A visit counter must keep counting for the life of the app. A per-request context must be shared within a request and fresh for the next one.

**Where:** [`samples/exercises/02-dependency-injection`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/02-dependency-injection) in the GitHub repo. Clone the repo, then work in the `Starter` project.

**Steps:**

1. Read the comments on `VisitCounter`, `RequestContext` and `RequestContextReporter` in `Starter/`. Each one says what it needs.
2. In `Starter/Program.cs`, register each service as a singleton, scoped or transient (TODOs 1–3).
3. Make `GET /visits` return the next count from `VisitCounter` (TODO 4).
4. Make `GET /request-context` return a `RequestContextReport` built from an injected `RequestContext` and `RequestContextReporter` (TODO 5).
5. Try a wrong lifetime on purpose, such as making `RequestContextReporter` a singleton, and read what the tests and the app tell you.

**Check your work:** the tests start red and turn green as you go. Each failure message names the lifetime that's wrong.

```sh
dotnet test --project samples/exercises/02-dependency-injection/Starter.Tests
```

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
