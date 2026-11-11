using HealthOrders;

var builder = WebApplication.CreateBuilder(args);

// Store:Online=false starts the store offline, to see the readiness check fail.
builder.Services.AddSingleton(new OrderStore { Online = builder.Configuration.GetValue("Store:Online", true) });
builder.Services.AddSingleton<Payments>();

// TODO 2: This check has no tags, so there's no way to give it to one endpoint and not the other.
//   Tag it "ready".
// TODO 3: Add the shutting-down check here, tagged "ready", and register the drain delay (see
//   Health.cs).
builder.Services.AddHealthChecks()
    .AddCheck<StoreHealthCheck>("store");

// TODO 4: An order takes 3 s, so a 1 s shutdown timeout cuts off any order in flight when the app
//   is told to stop: the host gives up waiting, and Kestrel drops the connection. Set it above the
//   drain delay plus the longest request, and below the platform's stop timeout, after which the
//   app is killed anyway (10 s for `docker stop`, 30 s by default on ECS). Say why in the comment.
// Keep shutdown short, so deploys are quick.
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(1));

var app = builder.Build();

// TODO 1: Liveness runs every check, the store's included, so when the store goes down, liveness
//   fails, and the orchestrator restarts an app that a restart can't fix. Give liveness no checks
//   (HealthCheckOptions.Predicate): if the app can answer, it's alive.
app.MapHealthChecks("/health/live");

// TODO 2: There's no readiness endpoint, so the load balancer can't tell that the app can't take
//   orders. Map /health/ready to the checks tagged "ready".

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
