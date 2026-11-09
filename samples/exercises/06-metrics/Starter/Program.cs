using Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
builder.Services.AddSingleton<OrderMetrics>();

var app = builder.Build();

app.MapPost("/orders", (PlaceOrderRequest order, IPaymentGateway payments, OrderMetrics metrics) =>
{
    var orderId = $"ord_{Guid.NewGuid():N}";

    // TODO 4: Count every order with metrics.OrderPlaced, once per order, with the outcome
    // "failed", "declined" or "accepted". For an accepted order, also call metrics.OrderAccepted
    // with its Total.
    // (Hint from a colleague: "add orderId as a tag too, then you can find any order on the
    // dashboard." Should you?)
    PaymentResult payment;
    try
    {
        payment = payments.Charge(orderId, order);
    }
    catch (PaymentGatewayException)
    {
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
