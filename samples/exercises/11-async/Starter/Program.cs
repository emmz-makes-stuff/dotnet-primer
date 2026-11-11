using AsyncOrders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<IWarehouse, SlowWarehouse>();
builder.Services.AddSingleton<OrderNotifications>();
builder.Services.AddHostedService<NotificationWorker>();

// This API works: it gives the right answers. But it wastes threads, keeps working for callers who
// have gone, loses failures, and makes callers wait longer than they need to.
// Run the tests to see what's wrong: dotnet test --project samples/exercises/11-async/Starter.Tests

var app = builder.Build();

// TODO 1: This handler is synchronous, so it can only wait for the warehouse by blocking:
//   .GetAwaiter().GetResult() holds the request's thread for as long as ReserveAsync takes. Under
//   load, the thread pool runs out of threads, and every request queues. Make the handler async, and
//   await the reservation. Keep the work in the handler: don't move it into a helper.
// TODO 2: The handler doesn't take the request's CancellationToken, so when the caller gives up, the
//   warehouse keeps working for nobody. Add a CancellationToken parameter (minimal APIs bind it to
//   HttpContext.RequestAborted), and pass it to ReserveAsync. Not to the notification: that runs
//   after the answer (TODO 3).
app.MapPost("/orders", (PlaceOrderRequest request, OrderStore store, IWarehouse warehouse) =>
{
    var problems = request.Problems();
    if (problems.Count > 0)
    {
        return Results.ValidationProblem(problems);
    }

    warehouse.ReserveAsync(request.Lines!, CancellationToken.None).GetAwaiter().GetResult();
    var order = store.Add(request.Lines!);

    // TODO 3: Nothing awaits this task, so if the notification fails, nobody ever knows. Hand the
    //   order to the OrderNotifications queue instead (take it as a parameter, and call Add). Its
    //   NotificationWorker, in OrderNotifications.cs, awaits each notification, and logs a failure
    //   at Error with the order id.
    _ = warehouse.NotifyAsync(order, CancellationToken.None);

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

    // TODO 4: These three calls don't depend on each other, but each one waits for the one before, so
    //   the quote takes as long as all three added up. Start all three, then await Task.WhenAll, and
    //   read each result with await (not .Result).
    var inStock = await warehouse.GetStockAsync(order.Lines, cancellationToken);
    var price = await warehouse.GetPriceAsync(order.Lines, cancellationToken);
    var shipping = await warehouse.GetShippingAsync(order.Lines, cancellationToken);

    return Results.Ok(new Quote(order.Id, inStock, price, shipping));
});

app.Run();

/// <summary>Visible to the tests' <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
