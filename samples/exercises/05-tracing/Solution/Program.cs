using System.Diagnostics;
using Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

var app = builder.Build();

app.MapPost("/orders", (PlaceOrderRequest order, IPaymentGateway payments) =>
{
    var orderId = $"ord_{Guid.NewGuid():N}";

    // A span for the business step. StartActivity makes it a child of the current span, the
    // incoming request's, and returns null when nothing is listening: hence the ?. calls.
    // The name is low-cardinality; the order id goes in an attribute.
    using var placeOrder = OrderTracing.Source.StartActivity("PlaceOrder");
    placeOrder?.SetTag("order.id", orderId);
    placeOrder?.SetTag("order.item_count", order.Items.Count);

    PaymentResult payment;
    // A child span for the outgoing call, so its time shows separately in the waterfall.
    using (var chargePayment = OrderTracing.Source.StartActivity("ChargePayment"))
    {
        try
        {
            payment = payments.Charge(orderId, order);
        }
        catch (PaymentGatewayException ex)
        {
            // Mark the span that failed, and record the exception on it as an "exception" event.
            chargePayment?.SetStatus(ActivityStatusCode.Error, ex.Message);
            chargePayment?.AddException(ex);
            return Results.Json(new PlaceOrderResponse(orderId, "failed", "the payment gateway is unavailable"), statusCode: StatusCodes.Status502BadGateway);
        }
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
