using HealthOrders;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Store:Online=false starts the store offline, to see the readiness check fail.
builder.Services.AddSingleton(new OrderStore { Online = builder.Configuration.GetValue("Store:Online", true) });
builder.Services.AddSingleton<Payments>();

builder.Services.AddHealthChecks()
    // TODO 2 (solved): tagged "ready", so the readiness endpoint runs it, and liveness doesn't.
    .AddCheck<StoreHealthCheck>("store", tags: ["ready"])
    // TODO 3 (solved): readiness fails once the app is told to stop.
    .AddCheck<ShuttingDownHealthCheck>("shutting-down", tags: ["ready"]);

// TODO 3 (solved): and the app keeps answering for a moment, so that the failure is seen.
builder.Services.AddHostedService<DrainDelay>();

// TODO 4 (solved): the shutdown timeout covers the drain delay (2 s) and the longest request, an
// order (3 s), with room to spare. It has to stay under the platform's stop timeout, after which
// the app is killed: 10 s for `docker stop`, and 30 s by default on ECS.
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(8));

var app = builder.Build();

// TODO 1 (solved): liveness runs no checks. If the app can answer, it's alive, and restarting it
// wouldn't bring the store back.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

// TODO 2 (solved): readiness runs the checks tagged "ready": can the app take traffic right now?
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapPost("/orders", async (PlaceOrderRequest request, OrderStore store, Payments payments,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Sku))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["sku"] = ["A SKU is required."] });
    }

    if (!store.Online)
    {
        return Results.Problem("The order store is offline.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    store.Begin();
    await payments.TakeAsync(cancellationToken);
    var order = store.Add(request.Sku);
    return Results.Created($"/orders/{order.Id}", order);
});

app.MapGet("/orders/{id:int}", (int id, OrderStore store) =>
    store.Find(id) is { } order ? Results.Ok(order) : Results.NotFound());

app.Run();

/// <summary>Visible to the tests' <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
