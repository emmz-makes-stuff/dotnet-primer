using AsyncOrders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<IWarehouse, SlowWarehouse>();
builder.Services.AddSingleton<OrderNotifications>();
builder.Services.AddHostedService<NotificationWorker>();

var app = builder.Build();

// TODO 1 (solved): the handler is async all the way. It awaits the warehouse, so no thread waits.
// TODO 2 (solved): it takes the request's CancellationToken (RequestAborted), and passes it on.
app.MapPost("/orders", async (PlaceOrderRequest request, OrderStore store, IWarehouse warehouse,
    OrderNotifications notifications, CancellationToken cancellationToken) =>
{
    var problems = request.Problems();
    if (problems.Count > 0)
    {
        return Results.ValidationProblem(problems);
    }

    await warehouse.ReserveAsync(request.Lines!, cancellationToken);
    var order = store.Add(request.Lines!);

    // TODO 3 (solved): the notification goes to the queue, and NotificationWorker awaits it, so a
    // failure is logged. The request doesn't wait for it, and its token doesn't cancel it.
    notifications.Add(order);

    return Results.Created($"/orders/{order.Id}", order);
});

app.MapGet("/orders/{id:int}", (int id, OrderStore store) =>
    store.Find(id) is { } order ? Results.Ok(order) : Results.NotFound());

app.MapGet("/orders/{id:int}/quote", async (int id, OrderStore store, IWarehouse warehouse,
    CancellationToken cancellationToken) =>
{
    if (store.Find(id) is not { } order)
    {
        return Results.NotFound();
    }

    // TODO 4 (solved): the three calls don't depend on each other, so start them all, then wait for
    // them together. The quote takes as long as the slowest call, not the sum of all three.
    var stock = warehouse.GetStockAsync(order.Lines, cancellationToken);
    var price = warehouse.GetPriceAsync(order.Lines, cancellationToken);
    var shipping = warehouse.GetShippingAsync(order.Lines, cancellationToken);
    await Task.WhenAll(stock, price, shipping);

    // They're all done, so these awaits don't wait. They read the results without .Result.
    return Results.Ok(new Quote(order.Id, await stock, await price, await shipping));
});

app.Run();

/// <summary>Visible to the tests' <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
