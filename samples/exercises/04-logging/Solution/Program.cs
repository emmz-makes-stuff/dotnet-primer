using Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

var app = builder.Build();

app.MapPost("/orders", (PlaceOrderRequest order, IPaymentGateway payments, ILogger<Program> logger) =>
{
    var orderId = $"ord_{Guid.NewGuid():N}";

    // Log identifiers, never personal data: the order id is enough to find the
    // customer when you need to, and it's safe to keep in a log store.
    PaymentResult payment;
    try
    {
        payment = payments.Charge(orderId, order);
    }
    catch (PaymentGatewayException ex)
    {
        OrderLog.PaymentFailed(logger, orderId, ex);
        return Results.Json(new PlaceOrderResponse(orderId, "failed", "the payment gateway is unavailable"), statusCode: StatusCodes.Status502BadGateway);
    }

    if (!payment.Accepted)
    {
        var reason = payment.DeclineReason ?? "unknown";
        OrderLog.PaymentDeclined(logger, orderId, reason);
        return Results.Json(new PlaceOrderResponse(orderId, "declined", reason), statusCode: StatusCodes.Status402PaymentRequired);
    }

    OrderLog.OrderPlaced(logger, orderId, order.Items.Count);
    return Results.Created($"/orders/{orderId}", new PlaceOrderResponse(orderId, "accepted"));
});

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
