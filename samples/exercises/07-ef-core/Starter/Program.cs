using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders;

var builder = WebApplication.CreateBuilder(args);

// TODO 1: register OrdersDbContext with AddDbContextPool, using SQL Server and the connection
// string named "Orders" from configuration. (Locally, keep that string in user secrets, not in
// appsettings.json.)

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// TODO 3: create the first migration from your model. In this folder, run:
//   dnx dotnet-ef@10.0.14 migrations add InitialCreate
// Then read what it generated in Migrations/ before you go on.

// [FromServices] makes the endpoints ask DI for the context. Without it, until TODO 1 is done,
// minimal APIs would treat an unregistered type as the request body and the app wouldn't start.

app.MapPost("/orders", async (PlaceOrderRequest request, [FromServices] OrdersDbContext db, CancellationToken ct) =>
{
    if (request.Items is not { Count: > 0 })
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["items"] = ["An order needs at least one item."],
        });
    }

    var order = new Order
    {
        CustomerEmail = request.CustomerEmail,
        Status = OrderStatus.Placed,
        PlacedAt = DateTimeOffset.UtcNow,
        Total = request.Items.Sum(i => i.Quantity * i.UnitPrice),
    };

    // TODO 4: this works, but saves the order, then each line, separately: if the third line
    // fails, the order is stored with only two. Save the order and its lines with one SaveChanges.
    db.Orders.Add(order);
    await db.SaveChangesAsync(ct);
    foreach (var item in request.Items)
    {
        order.Lines.Add(new OrderLine { Sku = item.Sku, Quantity = item.Quantity, UnitPrice = item.UnitPrice });
        await db.SaveChangesAsync(ct);
    }

    return Results.Created($"/orders/{order.Id}", new PlaceOrderResponse(order.Id));
});

app.MapGet("/customers/{email}/orders", async (string email, [FromServices] OrdersDbContext db, CancellationToken ct) =>
{
    // TODO 5: this works, but it's N+1: one query for the orders, then one more per order to count
    // its lines. Make it one query, with the line count in a projection.
    var orders = await db.Orders
        .Where(o => o.CustomerEmail == email)
        .OrderByDescending(o => o.PlacedAt)
        .ToListAsync(ct);

    var summaries = new List<OrderSummary>();
    foreach (var order in orders)
    {
        var lineCount = await db.Set<OrderLine>().CountAsync(l => l.OrderId == order.Id, ct);
        summaries.Add(new OrderSummary(order.Id, order.Status, order.Total, lineCount));
    }

    return summaries;
});

app.MapPost("/orders/{id:int}/cancel", (int id, [FromServices] OrdersDbContext db) =>
{
    // TODO 6: cancel the order through change tracking: load it, set its Status to Cancelled, and
    // save. Return 204 No Content, or 404 Not Found if there's no order with that id.
    return Results.StatusCode(StatusCodes.Status501NotImplemented);
});

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
