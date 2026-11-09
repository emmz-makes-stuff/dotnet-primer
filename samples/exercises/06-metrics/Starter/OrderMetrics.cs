using System.Diagnostics.Metrics;

namespace Orders;

/// <summary>The app's metrics. Registered as a singleton in Program.cs.</summary>
internal sealed class OrderMetrics
{
    public const string MeterName = "Orders";

    // TODO 1: Take an IMeterFactory in a constructor, create a Meter named MeterName from it,
    // and create two instruments (see the Metrics page):
    //   - a Counter<long> called orders.placed, with the unit "{order}";
    //   - a Histogram<double> called orders.value, with the unit "{GBP}".
    // Keep them in fields.

#pragma warning disable CA1822 // Empty until TODOs 2 and 3 use the instruments; then remove this and the restore.
    /// <summary>Counts an order, whatever happened to it.</summary>
    public void OrderPlaced(string paymentMethod, string outcome)
    {
        // TODO 2: Add 1 to orders.placed, tagged payment.method (use PaymentMethodTag) and
        // order.outcome.
    }

    /// <summary>Records an accepted order's value.</summary>
    public void OrderAccepted(string paymentMethod, decimal total)
    {
        // TODO 3: Record the total on orders.value, tagged payment.method (use PaymentMethodTag).
    }
#pragma warning restore CA1822

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
