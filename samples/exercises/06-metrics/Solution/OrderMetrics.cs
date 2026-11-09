using System.Diagnostics.Metrics;

namespace Orders;

/// <summary>
/// The app's metrics. The meter comes from <see cref="IMeterFactory"/>, so it's tied to the app's
/// lifetime and tests can listen to it; OpenTelemetry subscribes to it by name
/// (<c>.AddMeter("Orders")</c>). Register this class as a singleton.
/// </summary>
internal sealed class OrderMetrics
{
    public const string MeterName = "Orders";

    private readonly Counter<long> _ordersPlaced;
    private readonly Histogram<double> _orderValue;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        // A counter: it only goes up, and the backend works out the rate. The unit is a
        // curly-brace annotation, because "orders" isn't a unit of measure.
        _ordersPlaced = meter.CreateCounter<long>(
            "orders.placed", unit: "{order}", description: "Orders placed, by payment method and outcome.");

        // A histogram: a distribution, so you can ask for the median or the 95th percentile.
        // There's no standard currency unit, so the currency goes in an annotation too.
        _orderValue = meter.CreateHistogram<double>(
            "orders.value", unit: "{GBP}", description: "The value of accepted orders.");
    }

    /// <summary>Counts an order, whatever happened to it.</summary>
    public void OrderPlaced(string paymentMethod, string outcome) =>
        _ordersPlaced.Add(
            1,
            new KeyValuePair<string, object?>("payment.method", PaymentMethodTag(paymentMethod)),
            new KeyValuePair<string, object?>("order.outcome", outcome));

    /// <summary>Records an accepted order's value.</summary>
    public void OrderAccepted(string paymentMethod, decimal total) =>
        _orderValue.Record(
            (double)total,
            new KeyValuePair<string, object?>("payment.method", PaymentMethodTag(paymentMethod)));

    /// <summary>
    /// The payment method as a tag value. It comes from the client, so it could be anything: map it
    /// to a fixed set, or every new value becomes a new time series.
    /// </summary>
    internal static string PaymentMethodTag(string paymentMethod) =>
        paymentMethod.ToUpperInvariant() switch
        {
            "CARD" => "card",
            "VOUCHER" => "voucher",
            _ => "other",
        };
}
