namespace Orders;

/// <summary>Takes payment for an order. Provided for the exercise: you don't need to change it.</summary>
public interface IPaymentGateway
{
    /// <summary>Charges the customer. Returns whether the payment was accepted, and why not if it wasn't.</summary>
    /// <exception cref="PaymentGatewayException">The gateway itself failed.</exception>
    PaymentResult Charge(string orderId, PlaceOrderRequest order);
}

/// <summary>The outcome of a charge.</summary>
public sealed record PaymentResult(bool Accepted, string? DeclineReason = null);

/// <summary>Thrown when the payment gateway fails, as opposed to declining a payment.</summary>
public sealed class PaymentGatewayException(string message) : Exception(message);

/// <summary>
/// A fake gateway with predictable behaviour:
/// <list type="bullet">
///   <item>vouchers are declined;</item>
///   <item>customers at <c>@fail.test</c> make the gateway itself fail;</item>
///   <item>everything else is accepted.</item>
/// </list>
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    public PaymentResult Charge(string orderId, PlaceOrderRequest order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.CustomerEmail.EndsWith("@fail.test", StringComparison.OrdinalIgnoreCase))
        {
            throw new PaymentGatewayException("The payment gateway timed out.");
        }

        return string.Equals(order.PaymentMethod, "voucher", StringComparison.OrdinalIgnoreCase)
            ? new PaymentResult(Accepted: false, DeclineReason: "vouchers are not accepted")
            : new PaymentResult(Accepted: true);
    }
}
