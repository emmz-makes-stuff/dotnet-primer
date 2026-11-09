---
title: "ASP.NET Core"
description: "The web framework at the heart of the learning path: the host, WebApplication, minimal APIs, route groups and typed results."
learn:
  - title: "Overview of ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/overview
  - title: "APIs overview: minimal APIs or controllers"
    url: https://learn.microsoft.com/aspnet/core/fundamentals/apis
  - title: "Minimal APIs quick reference"
    url: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis
  - title: "WebApplication and WebApplicationBuilder"
    url: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/webapplication
---

By the end of this page you'll be able to read any modern ASP.NET Core `Program.cs`, write HTTP endpoints that bind route, query and body values, group them, and return results that say exactly which status codes they produce. Everything else on the learning path (configuration, dependency injection, middleware, logging) plugs into the shape you'll see here.

## An app is a console program

An ASP.NET Core app is a plain console program that hosts its own web server, [Kestrel](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel). There is no IIS pipeline it lives inside, no `Global.asax`, and no `web.config` for app settings. It runs the same way with `dotnet run` on a laptop, in a Linux container, or behind a reverse proxy.

This is a complete app:

```csharp title="Program.cs"
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "Hello, .NET 10!");

app.Run();
```

`dotnet new web` creates exactly that. There is no `Main` method because C# lets the file's top-level statements be the entry point.

## Builder, build, run

Every `Program.cs` has the same three phases, always in this order:

1. **`WebApplication.CreateBuilder(args)`** sets up the **host**: [configuration](/aspnet-core/configuration/) (`appsettings.json`, environment variables, command line), logging, and the [dependency injection](/aspnet-core/dependency-injection/) container. You add to them through `builder.Configuration`, `builder.Logging` and `builder.Services`.
2. **`builder.Build()`** freezes the services and creates the `WebApplication`. After this point you can't register services any more.
3. **`app.Map…` and `app.Use…`** describe how requests are handled: endpoints, and the [middleware](/aspnet-core/middleware/) pipeline that runs in front of them. **`app.Run()`** starts Kestrel and blocks until the app is told to stop.

```csharp title="Program.cs"
var builder = WebApplication.CreateBuilder(args);

// 1. Services and configuration go here.
builder.Services.AddProblemDetails();

var app = builder.Build();

// 2. Middleware and endpoints go here.
app.UseExceptionHandler();
app.MapGet("/", () => "Hello");

// 3. Start listening.
app.Run();
```

The host also gives you an **environment** (`Development`, `Staging`, `Production`, from the `ASPNETCORE_ENVIRONMENT` variable) in `builder.Environment` and `app.Environment`, and graceful shutdown when the process gets Ctrl+C or `SIGTERM`.

## Minimal API endpoints

An endpoint is an HTTP method, a route template, and a handler: any delegate, usually a lambda. ASP.NET Core works out where each handler parameter comes from:

```csharp title="Program.cs"
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/orders/{id:int}", (int id) => $"Order {id}");

app.MapGet("/search", (string q, int page = 1) => $"Searching '{q}', page {page}");

app.MapPost("/orders", (Order order) => TypedResults.Created($"/orders/{order.Id}", order));

app.MapGet("/whoami", (HttpContext context) => context.User.Identity?.Name ?? "anonymous");

app.Run();

public sealed record Order(int Id, string Product, int Quantity);
```

| Parameter                                                            | Bound from            |
| -------------------------------------------------------------------- | --------------------- |
| Name matches a route segment (`{id}`)                                | the route             |
| Simple type (`string`, `int`, `Guid`…) not in the route              | the query string      |
| Complex type (`Order`) on `POST`/`PUT`/`PATCH`                       | the JSON request body |
| A registered service                                                 | dependency injection  |
| `HttpContext`, `HttpRequest`, `CancellationToken`, `ClaimsPrincipal` | the current request   |

The `:int` in `{id:int}` is a **route constraint**: `/orders/abc` doesn't match this endpoint, so it returns 404 instead of a binding error. Required parameters that are missing (no `q` on `/search`) get a `400 Bad Request` without your handler running. Make a parameter optional with a default value or a nullable type (`int? page`).

When the source would be ambiguous, say where it comes from with an attribute: `[FromRoute]`, `[FromQuery]`, `[FromHeader]`, `[FromBody]`, `[FromServices]`.

A handler can return a string (sent as `text/plain`), any other object (serialized to JSON with `System.Text.Json`), or an `IResult` that controls the status code and body.

## Typed results

`TypedResults` builds results whose **type** records what they produce, so the compiler checks your handler and the OpenAPI document can describe it. When a handler returns different results on different paths, declare them all with `Results<…>`:

```csharp title="Program.cs"
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var todos = new ConcurrentDictionary<int, Todo>();
var nextId = 0;

var group = app.MapGroup("/todos");

group.MapGet("/", () => TypedResults.Ok(todos.Values.OrderBy(t => t.Id)));

group.MapGet("/{id:int}", Results<Ok<Todo>, NotFound> (int id) =>
    todos.TryGetValue(id, out var todo) ? TypedResults.Ok(todo) : TypedResults.NotFound());

group.MapPost("/", (NewTodo input) =>
{
    var todo = new Todo(Interlocked.Increment(ref nextId), input.Title);
    todos[todo.Id] = todo;
    return TypedResults.Created($"/todos/{todo.Id}", todo);
});

app.Run();

public sealed record Todo(int Id, string Title);

public sealed record NewTodo(string Title);
```

`Results<Ok<Todo>, NotFound>` says "this handler returns 200 with a `Todo`, or 404". Return anything else and it won't compile. Prefer `TypedResults` over the older `Results` class (`Results.Ok(…)`), which returns a plain `IResult` and hides that information.

The in-memory dictionary is only there to keep the example to one file. The [Dependency Injection](/aspnet-core/dependency-injection/) page shows how to move it into a service.

## Route groups

`MapGroup` gives a set of endpoints a shared route prefix, as `/todos` does above. Anything you configure on the group applies to every endpoint in it, which keeps cross-cutting settings in one place:

```csharp title="Program.cs"
var orders = app.MapGroup("/orders")
    .RequireAuthorization()
    .WithTags("Orders");

orders.MapGet("/", ListOrders);
orders.MapGet("/{id:int}", GetOrder);
orders.MapPost("/", CreateOrder);
```

Groups nest, so `/api` → `/api/orders` → `/api/orders/{id}/lines` can each add their own settings. For a larger API, put each group in an extension method (`app.MapOrders();`) in its own file, so `Program.cs` stays short.

## Controllers are still there

Minimal APIs are the default for new HTTP APIs, and this primer uses them throughout. **Controllers** (`[ApiController]` classes deriving from `ControllerBase`, with action methods) are fully supported too:

```csharp title="Program.cs"
builder.Services.AddControllers();
// ...
app.MapControllers();
```

Microsoft recommends minimal APIs for new projects. Reach for controllers when you need what they give you out of the box:

- model binding extensibility (`IModelBinderProvider`);
- advanced validation (`IModelValidator`);
- application parts or the application model;
- OData.

Controllers also suit a team that already knows MVC conventions well, or a large controller-based API you're bringing across. Both styles can live in the same app. For server-rendered HTML, look at Razor Pages or Blazor instead. They're out of scope here.

## Pitfalls

- **Registering services after `Build()`.** `builder.Services.Add…` after `builder.Build()` throws `InvalidOperationException: The service collection cannot be modified because it is read-only`. Fix: do all registration before `Build()`.
- **Forgetting `app.Run()`.** The program reaches the end of `Program.cs` and exits without listening. Fix: end with `app.Run()` (or `await app.RunAsync()`).
- **Expecting `/orders/abc` to give a 400.** With `{id:int}`, a non-numeric segment doesn't match the route at all, so it's a 404. Without the constraint, binding fails and it's a 400. Pick the behaviour you want, and know which one you have.
- **Returning `Results.Ok(…)` everywhere.** It works, but your OpenAPI document and tests can't see the response types. Fix: use `TypedResults` and `Results<…>`.
- **Blocking on async work.** `.Result` or `.Wait()` inside a handler ties up a thread for the whole call and can exhaust the thread pool under load. Fix: make the handler `async` and `await`. Accept a `CancellationToken` parameter and pass it on, so abandoned requests stop work early.
- **One giant `Program.cs`.** Endpoints pile up fast. Fix: move each area into a `MapGroup` extension method in its own file.

## Where next

The next three pages fill in the builder and the pipeline you've just seen:

1. **[Configuration](/aspnet-core/configuration/)**: where `builder.Configuration` gets its values, and binding them to typed options.
2. **[Dependency Injection](/aspnet-core/dependency-injection/)**: what `builder.Services` is, and how handlers get their services.
3. **[Middleware](/aspnet-core/middleware/)**: the pipeline that every request passes through before it reaches an endpoint.
