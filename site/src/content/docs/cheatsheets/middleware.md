---
title: Middleware cheatsheet
description: Pipeline order, Use/Run/Map, middleware templates, Problem Details and endpoint filters, on one page.
prev: false
next: false
learn:
  - title: ASP.NET Core middleware
    url: https://learn.microsoft.com/aspnet/core/fundamentals/middleware/
  - title: Write custom ASP.NET Core middleware
    url: https://learn.microsoft.com/aspnet/core/fundamentals/middleware/write
  - title: Filters in minimal API apps
    url: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters
---

Back to [Middleware](/aspnet-core/middleware/).

## Recommended order

```csharp title="Program.cs"
app.UseExceptionHandler();      // catches exceptions from everything below
app.UseHsts();                  // non-Development only
app.UseHttpsRedirection();
app.UseStaticFiles();           // short-circuits for files in wwwroot
app.UseRouting();               // selects the endpoint
app.UseCors();                  // before auth, so preflight requests get through
app.UseAuthentication();
app.UseAuthorization();         // needs the endpoint routing selected
app.UseMyMiddleware();          // your middleware: sees context.User
app.MapGet("/", () => "Hello"); // endpoints
```

`WebApplication` adds the developer exception page (in Development), `UseRouting`, and
`UseAuthentication`/`UseAuthorization` (when registered) automatically. Call them yourself to control
where they go.

## Shapes

```csharp title="Program.cs"
app.Use(async (context, next) =>                      // in, next, out
{
    // before
    await next(context);
    // after (don't change headers here; see OnStarting)
});

app.Run(context => context.Response.WriteAsync("end")); // terminal: no next

app.Map("/admin", admin => admin.UseMiddleware<AdminAuditMiddleware>()); // branch by path prefix

app.UseWhen(context => context.Request.Query.ContainsKey("debug"),        // branch by condition,
    debug => debug.UseMiddleware<DebugHeadersMiddleware>());              // then rejoin
```

## Middleware class

```csharp title="RequestTimingMiddleware.cs"
public sealed class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    // Constructor: singletons only (middleware is created once).
    // InvokeAsync parameters: scoped services, resolved per request.
    public async Task InvokeAsync(HttpContext context, IRequestAudit audit)
    {
        await next(context);
        audit.Record(context.Request.Path, context.Response.StatusCode);
    }
}

public static class RequestTimingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestTimingMiddleware>();
}
```

## Short-circuit

```csharp
if (!context.Request.Headers.ContainsKey("X-Api-Key"))
{
    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    return;                                   // don't call next
}
await next(context);
```

```csharp title="Program.cs"
app.MapGet("/health", () => "Healthy").ShortCircuit();   // run as soon as routing selects it
```

## Response headers that always arrive

```csharp
context.Response.OnStarting(() =>
{
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    return Task.CompletedTask;
});
await next(context);
```

Set directly, a header is wiped from error responses: the exception handler clears the response first.
`context.Response.HasStarted` tells you if headers have already been sent.

## Errors as Problem Details

```csharp title="Program.cs"
builder.Services.AddProblemDetails();                        // application/problem+json
builder.Services.AddExceptionHandler<OrderNotFoundHandler>(); // optional: map known exceptions

app.UseExceptionHandler();      // unhandled exception → 500 Problem Details, logged
app.UseStatusCodePages();       // empty 4xx/5xx → Problem Details body
```

```csharp title="OrderNotFoundHandler.cs"
public sealed class OrderNotFoundHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not OrderNotFoundException notFound) return false;   // not ours
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = { Title = "Order not found", Detail = notFound.Message },
            Exception = exception,
        });
    }
}
```

## Endpoint filters

```csharp title="Program.cs"
app.MapPost("/orders", (NewOrder order) => TypedResults.Created($"/orders/{order.Product}", order))
    .AddEndpointFilter(async (context, next) =>
    {
        var order = context.GetArgument<NewOrder>(0);
        return order.Quantity > 0
            ? await next(context)
            : TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["quantity"] = ["Quantity must be at least 1."],
            });
    });

app.MapGroup("/admin").AddEndpointFilter<AuditFilter>();     // class: IEndpointFilter
```

| Middleware                       | Endpoint filter                          |
| -------------------------------- | ---------------------------------------- |
| every request (or a path prefix) | selected endpoints or groups             |
| raw `HttpContext`                | bound arguments and the handler's result |
| runs even if no endpoint matches | runs only for its endpoint               |
