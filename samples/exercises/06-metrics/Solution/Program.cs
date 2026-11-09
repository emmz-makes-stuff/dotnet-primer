using Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
builder.Services.AddSingleton<OrderMetrics>();

var app = builder.Build();

app.MapPost("/orders", (PlaceOrderRequest order, IPaymentGateway payments, OrderMetrics metrics) =>
{
    var orderId = $"ord_{Guid.NewGuid():N}";

    // Tags are bounded values you'd group by: the payment method and the outcome. Never the
    // order id or the customer: each distinct value would be a new time series.
    PaymentResult payment;
    try
    {
        payment = payments.Charge(orderId, order);
    }
    catch (PaymentGatewayException)
    {
        metrics.OrderPlaced(order.PaymentMethod, "failed");
        return Results.Json(new PlaceOrderResponse(orderId, "failed", "the payment gateway is unavailable"), statusCode: StatusCodes.Status502BadGateway);
    }

    if (!payment.Accepted)
    {
        metrics.OrderPlaced(order.PaymentMethod, "declined");
        var reason = payment.DeclineReason ?? "unknown";
        return Results.Json(new PlaceOrderResponse(orderId, "declined", reason), statusCode: StatusCodes.Status402PaymentRequired);
    }

    metrics.OrderPlaced(order.PaymentMethod, "accepted");
    metrics.OrderAccepted(order.PaymentMethod, order.Total);
    return Results.Created($"/orders/{orderId}", new PlaceOrderResponse(orderId, "accepted"));
});

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
