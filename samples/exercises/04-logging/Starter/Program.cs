using Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

var app = builder.Build();

app.MapPost("/orders", (PlaceOrderRequest order, IPaymentGateway payments, ILogger<Program> logger) =>
{
    var orderId = $"ord_{Guid.NewGuid():N}";

    // This endpoint works, but its logging is the kind that makes production incidents hard:
    // interpolated strings (so nothing is a searchable property), the wrong levels, a failure
    // logged twice, and the customer's email address in the logs.
    PaymentResult payment;
    try
    {
        payment = payments.Charge(orderId, order);
    }
    catch (PaymentGatewayException ex)
    {
        // This one is already right: one entry, at Error, with the exception and the order id.
        OrderLog.PaymentFailed(logger, orderId, ex);
        return Results.Json(new PlaceOrderResponse(orderId, "failed", "the payment gateway is unavailable"), statusCode: StatusCodes.Status502BadGateway);
    }

#pragma warning disable CA1848, CA1873, CA2254 // Deliberately bad: fixing these calls is the exercise.
    if (!payment.Accepted)
    {
        var reason = payment.DeclineReason ?? "unknown";
        // TODO 2: Replace these two calls with one OrderLog.PaymentDeclined call.
        logger.LogError($"Payment failed for {order.CustomerEmail}");
        logger.LogError($"Order {orderId} declined: {reason}");
        return Results.Json(new PlaceOrderResponse(orderId, "declined", reason), statusCode: StatusCodes.Status402PaymentRequired);
    }

    // TODO 3: Replace this call with OrderLog.OrderPlaced. Log the order id, not the email.
    logger.LogInformation($"Order {orderId} placed by {order.CustomerEmail} with {order.Items.Count} items");
#pragma warning restore CA1848, CA1873, CA2254
    return Results.Created($"/orders/{orderId}", new PlaceOrderResponse(orderId, "accepted"));
});

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
