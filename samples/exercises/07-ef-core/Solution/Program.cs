using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Orders;

var builder = WebApplication.CreateBuilder(args);

// Pooled: each request still gets its own context, and its change tracker, but a finished request's
// context is reset and reused instead of being built again.
builder.Services.AddDbContextPool<OrdersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Orders")));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.MapPost("/orders", async (PlaceOrderRequest request, OrdersDbContext db, CancellationToken ct) =>
{
    if (request.Validate() is { Count: > 0 } errors)
    {
        return Results.ValidationProblem(errors);
    }

    var order = new Order
    {
        CustomerEmail = request.CustomerEmail,
        Status = OrderStatus.Placed,
        PlacedAt = DateTimeOffset.UtcNow,
        Total = request.Items.Sum(i => i.Quantity * i.UnitPrice),
    };
    foreach (var item in request.Items)
    {
        order.Lines.Add(new OrderLine { Sku = item.Sku, Quantity = item.Quantity, UnitPrice = item.UnitPrice });
    }

    // One SaveChanges is one unit of work: the order and its lines are saved together, or not at all.
    db.Orders.Add(order);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/orders/{order.Id}", new PlaceOrderResponse(order.Id));
});

app.MapGet("/orders/{id:int}", async (int id, OrdersDbContext db, CancellationToken ct) =>
    // Read-only, so not tracked. Include loads the lines with the order, in one query.
    await db.Orders.AsNoTracking().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, ct) is { } order
        ? Results.Ok(order)
        : Results.NotFound());

app.MapGet("/customers/{email}/orders", async (string email, OrdersDbContext db, CancellationToken ct) =>
    // A projection is one SQL command, with the line count as a subquery. It's never tracked,
    // because it returns no entities.
    await db.Orders
        .Where(o => o.CustomerEmail == email)
        .OrderByDescending(o => o.PlacedAt)
        .Select(o => new OrderSummary(o.Id, o.Status, o.Total, o.Lines.Count))
        .ToListAsync(ct));

app.MapPost("/orders/{id:int}/cancel", async (int id, OrdersDbContext db, CancellationToken ct) =>
{
    // FindAsync returns a tracked entity, so changing it is all SaveChanges needs.
    var order = await db.Orders.FindAsync([id], ct);
    if (order is null)
    {
        return Results.NotFound();
    }

    order.Status = OrderStatus.Cancelled;
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
