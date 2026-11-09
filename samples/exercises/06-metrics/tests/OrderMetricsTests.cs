using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Orders;

namespace Orders.Tests;

/// <summary>
/// Shared tests for exercise 06. Compiled into both Starter.Tests and
/// Solution.Tests, so they run against whichever app the project references.
/// Against the starter they fail until its TODOs are done.
/// </summary>
/// <remarks>
/// Each test makes its own <see cref="MetricCollector{T}"/>s before placing orders, so it sees only
/// its own measurements. Tests in one class run one at a time.
/// </remarks>
public sealed class OrderMetricsTests(WebApplicationFactory<Program> app) : IClassFixture<WebApplicationFactory<Program>>
{
    internal const string MeterName = "Orders";
    internal const string Email = "jo.bloggs@example.test";
    internal const string FailingEmail = "sam.jones@fail.test";

    internal static readonly string[] AllowedTagKeys = ["payment.method", "order.outcome"];

    [Fact]
    public async Task OrdersAreCountedByBoundedTags()
    {
        using var placed = Collector<long>("orders.placed");

        await PlaceOrderAsync("card", HttpStatusCode.Created);
        await PlaceOrderAsync("card", HttpStatusCode.Created);
        await PlaceOrderAsync("voucher", HttpStatusCode.PaymentRequired);

        var measurements = placed.GetMeasurementSnapshot();
        Assert.True(
            measurements.Count > 0,
            "Nothing was recorded on an orders.placed instrument. Create a Meter named \"Orders\" through " +
            "IMeterFactory, create a Counter<long> called orders.placed, and Add(1, …) for every order.");
        Assert.True(
            placed.Instrument?.Unit == "{order}",
            $"orders.placed should declare its unit as \"{{order}}\", but it was \"{placed.Instrument?.Unit}\".");

        var cardAccepted = Sum(measurements, "card", "accepted");
        var voucherDeclined = Sum(measurements, "voucher", "declined");
        Assert.True(
            cardAccepted == 2 && voucherDeclined == 1,
            $"Expected orders.placed to count 2 for payment.method=card, order.outcome=accepted and 1 for " +
            $"payment.method=voucher, order.outcome=declined, but it counted {cardAccepted} and {voucherDeclined}. " +
            "Tag every measurement with both.");
    }

    [Fact]
    public async Task OrderValuesAreRecordedInAHistogram()
    {
        using var value = Collector<double>("orders.value");

        await PlaceOrderAsync("card", HttpStatusCode.Created, total: 42.50m);

        var measurements = value.GetMeasurementSnapshot();
        Assert.True(
            measurements.Count == 1,
            $"Expected one orders.value measurement for the accepted order, but found {measurements.Count}. " +
            "Create a Histogram<double> called orders.value, and Record the order's Total when it's accepted.");
        Assert.True(
            measurements[0].Value == 42.50,
            $"orders.value should record the order's total, 42.50, but recorded {measurements[0].Value}.");
        Assert.True(
            value.Instrument?.Unit == "{GBP}",
            $"orders.value should declare its unit as \"{{GBP}}\", but it was \"{value.Instrument?.Unit}\".");
    }

    [Fact]
    public async Task IdentifiersAreNotTags()
    {
        using var placed = Collector<long>("orders.placed");
        using var value = Collector<double>("orders.value");

        var orderIds = new[]
        {
            await PlaceOrderAsync("card", HttpStatusCode.Created),
            await PlaceOrderAsync("voucher", HttpStatusCode.PaymentRequired),
            await PlaceOrderAsync("card", HttpStatusCode.BadGateway, email: FailingEmail),
        };

        var tags = placed.GetMeasurementSnapshot().SelectMany(m => m.Tags)
            .Concat(value.GetMeasurementSnapshot().SelectMany(m => m.Tags))
            .ToList();
        Assert.True(tags.Count > 0, "Nothing was recorded on orders.placed or orders.value.");

        var unexpectedKeys = tags.Select(t => t.Key).Distinct().Except(AllowedTagKeys).ToList();
        Assert.True(
            unexpectedKeys.Count == 0,
            $"Unexpected tag keys: {string.Join(", ", unexpectedKeys)}. Only payment.method and order.outcome are " +
            "allowed. Every distinct tag value makes a new time series, so an order id or a customer as a tag " +
            "means one series per order: put identifiers in logs and traces instead.");

        var leaked = tags.Where(t => t.Value is string s && (orderIds.Contains(s) || s is Email or FailingEmail)).ToList();
        Assert.True(
            leaked.Count == 0,
            $"A tag carried an order id or an email ({leaked.FirstOrDefault().Key}). Identifiers are not tags.");
    }

    [Fact]
    public async Task UnknownPaymentMethodsShareOneTagValue()
    {
        using var placed = Collector<long>("orders.placed");

        await PlaceOrderAsync("Gift-Card-7f3a", HttpStatusCode.Created);

        var methods = placed.GetMeasurementSnapshot()
            .Select(m => m.Tags.TryGetValue("payment.method", out var v) ? v?.ToString() : null)
            .ToList();
        Assert.True(
            methods is ["other"],
            $"An unknown payment method should be tagged payment.method=other, but was [{string.Join(", ", methods)}]. " +
            "The client chooses the payment method, so use OrderMetrics.PaymentMethodTag to keep the values bounded.");
    }

    private MetricCollector<T> Collector<T>(string instrument)
        where T : struct =>
        new(app.Services.GetRequiredService<IMeterFactory>(), MeterName, instrument);

    private static long Sum(IEnumerable<CollectedMeasurement<long>> measurements, string method, string outcome) =>
        measurements
            .Where(m => m.Tags.TryGetValue("payment.method", out var p) && p as string == method
                && m.Tags.TryGetValue("order.outcome", out var o) && o as string == outcome)
            .Sum(m => m.Value);

    private async Task<string> PlaceOrderAsync(
        string paymentMethod, HttpStatusCode expected, decimal total = 10m, string email = Email)
    {
        using var client = app.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/orders",
            new PlaceOrderRequest(email, paymentMethod, [new OrderItem("SKU-1", 1)], total),
            TestContext.Current.CancellationToken);

        Assert.True(
            response.StatusCode == expected,
            $"POST /orders with {paymentMethod} should return {(int)expected}, but returned {(int)response.StatusCode}.");
        var body = await response.Content.ReadFromJsonAsync<PlaceOrderResponse>(TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrEmpty(body?.OrderId), "The response should include the order id.");
        return body!.OrderId;
    }
}
