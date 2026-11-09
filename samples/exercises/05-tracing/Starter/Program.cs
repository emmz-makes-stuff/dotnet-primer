using System.Diagnostics;
using Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

var app = builder.Build();

app.MapPost("/orders", (PlaceOrderRequest order, IPaymentGateway payments) =>
{
    var orderId = $"ord_{Guid.NewGuid():N}";

    // TODO 2: Start a span named PlaceOrder for the whole order (with `using var`, so it ends
    // when the order does). Give it the attributes order.id and order.item_count.

    PaymentResult payment;
    // TODO 3: Put the payment step in a child span named ChargePayment, with a `using` block
    // around this try/catch.
    try
    {
        payment = payments.Charge(orderId, order);
    }
    catch (PaymentGatewayException)
    {
        // TODO 4: Mark the ChargePayment span as failed (SetStatus) and record the exception
        // on it (AddException).
        return Results.Json(new PlaceOrderResponse(orderId, "failed", "the payment gateway is unavailable"), statusCode: StatusCodes.Status502BadGateway);
    }

    if (!payment.Accepted)
    {
        var reason = payment.DeclineReason ?? "unknown";
        return Results.Json(new PlaceOrderResponse(orderId, "declined", reason), statusCode: StatusCodes.Status402PaymentRequired);
    }

    return Results.Created($"/orders/{orderId}", new PlaceOrderResponse(orderId, "accepted"));
});

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
