using TestingOrders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrders();

var app = builder.Build();

app.MapPost("/orders", (PlaceOrderRequest request, OrderService orders) =>
{
    var problems = request.Problems();
    if (problems.Count > 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [.. problems] });
    }

    var order = orders.Place(request.CustomerEmail!, request.Lines!);
    return Results.Created($"/orders/{order.Id}", order);
});

app.MapGet("/orders/{id:int}", (int id, OrderStore store) =>
    store.Find(id) is { } order ? Results.Ok(order) : Results.NotFound());

app.MapPost("/orders/{id:int}/cancel", async (int id, OrderService orders, CancellationToken cancellationToken) =>
    await orders.CancelAsync(id, cancellationToken) switch
    {
        CancelOutcome.Cancelled => Results.NoContent(),
        CancelOutcome.NotFound => Results.NotFound(),
        CancelOutcome.AlreadyCancelled => Results.Conflict(new { error = "The order is already cancelled." }),
        CancelOutcome.WindowClosed => Results.Conflict(new { error = $"Orders can only be cancelled within {CancelPolicy.Window.TotalMinutes} minutes of being placed." }),
        var outcome => throw new InvalidOperationException($"Unknown cancel outcome {outcome}."),
    });

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
