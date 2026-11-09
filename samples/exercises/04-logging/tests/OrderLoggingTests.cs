using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Orders;

namespace Orders.Tests;

/// <summary>
/// Shared tests for exercise 04. Compiled into both Starter.Tests and
/// Solution.Tests, so they run against whichever app the project references.
/// Against the starter they fail until its TODOs are done.
/// </summary>
public sealed class OrderLoggingTests(OrderLoggingTests.App app) : IClassFixture<OrderLoggingTests.App>
{
    /// <summary>The customer email every test order uses.</summary>
    internal const string Email = "jo.bloggs@example.test";

    /// <summary>A customer email that makes the fake payment gateway fail.</summary>
    internal const string FailingEmail = "sam.jones@fail.test";

    /// <summary>The app, with every log entry captured in memory.</summary>
    public sealed class App : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureLogging(logging => logging.AddFakeLogging());

        public FakeLogCollector Logs => Services.GetFakeLogCollector();
    }

    [Fact]
    public async Task PlacedOrderIsLoggedOnceWithStructuredProperties()
    {
        var orderId = await PlaceOrderAsync("card", HttpStatusCode.Created, itemCount: 3);

        var entries = EntriesFor(orderId).Where(r => r.Level == LogLevel.Information).ToList();
        Assert.True(
            entries.Count == 1,
            $"Expected exactly one Information log with an OrderId property of {orderId} for the placed order, " +
            $"but found {entries.Count}. Log it through a [LoggerMessage] method (or a message template), " +
            "so OrderId is a structured property rather than text inside the message.");
        Assert.True(
            Property(entries[0], "ItemCount") == "3",
            $"The \"order placed\" log should have an ItemCount property of 3, but it was \"{Property(entries[0], "ItemCount")}\".");
    }

    [Fact]
    public async Task DeclinedPaymentIsLoggedOnceAsAWarning()
    {
        var orderId = await PlaceOrderAsync("voucher", HttpStatusCode.PaymentRequired);

        var entries = EntriesFor(orderId).ToList();
        var warnings = entries.Where(r => r.Level == LogLevel.Warning).ToList();
        Assert.True(
            warnings.Count == 1,
            $"Expected exactly one Warning log with an OrderId property of {orderId} for the declined payment, " +
            $"but found {warnings.Count}. A declined payment is expected and handled: log it once, as a Warning.");
        Assert.True(
            !string.IsNullOrWhiteSpace(Property(warnings[0], "Reason")),
            "The \"payment declined\" log should have a Reason property saying why.");
        Assert.True(
            !entries.Any(r => r.Level >= LogLevel.Error),
            "A declined payment was logged at Error. Nothing failed: the app handled it. " +
            "Keep Error for a unit of work that failed and may need someone to act.");
    }

    [Fact]
    public async Task GatewayFailureIsLoggedOnceAsAnErrorWithTheException()
    {
        var orderId = await PlaceOrderAsync("card", HttpStatusCode.BadGateway, email: FailingEmail);

        var errors = EntriesFor(orderId).Where(r => r.Level == LogLevel.Error).ToList();
        Assert.True(
            errors.Count == 1,
            $"Expected exactly one Error log with an OrderId property of {orderId} for the gateway failure, " +
            $"but found {errors.Count}. The provided OrderLog.PaymentFailed call should stay as it is.");
        Assert.True(
            errors[0].Exception is PaymentGatewayException,
            "The \"payment failed\" log should carry the PaymentGatewayException as its exception.");
    }

    [Fact]
    public async Task CustomerEmailNeverAppearsInTheLogs()
    {
        await PlaceOrderAsync("card", HttpStatusCode.Created);
        await PlaceOrderAsync("voucher", HttpStatusCode.PaymentRequired);
        await PlaceOrderAsync("card", HttpStatusCode.BadGateway, email: FailingEmail);

        var leaks = app.Logs.GetSnapshot()
            .Where(r => Mentions(r, Email) || Mentions(r, FailingEmail))
            .Select(r => $"[{r.Level}] {r.Message}")
            .ToList();
        Assert.True(
            leaks.Count == 0,
            $"The customer's email address appeared in {leaks.Count} log entries, for example: {leaks.FirstOrDefault()}. " +
            "Email addresses are personal data: log the order id instead.");
    }

    private async Task<string> PlaceOrderAsync(
        string paymentMethod, HttpStatusCode expected, int itemCount = 1, string email = Email)
    {
        using var client = app.CreateClient();
        var items = Enumerable.Range(1, itemCount).Select(i => new OrderItem($"SKU-{i}", 1)).ToList();
        using var response = await client.PostAsJsonAsync(
            "/orders",
            new PlaceOrderRequest(email, paymentMethod, items, 42.50m),
            TestContext.Current.CancellationToken);

        Assert.True(
            response.StatusCode == expected,
            $"POST /orders with {paymentMethod} should return {(int)expected}, but returned {(int)response.StatusCode}.");
        var body = await response.Content.ReadFromJsonAsync<PlaceOrderResponse>(TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrEmpty(body?.OrderId), "The response should include the order id.");
        return body!.OrderId;
    }

    /// <summary>Entries whose structured OrderId property is this order's id.</summary>
    private IEnumerable<FakeLogRecord> EntriesFor(string orderId) =>
        app.Logs.GetSnapshot().Where(r => Property(r, "OrderId") == orderId);

    private static string? Property(FakeLogRecord record, string name) =>
        record.StructuredState?.FirstOrDefault(p => p.Key == name).Value;

    private static bool Mentions(FakeLogRecord record, string text) =>
        record.Message.Contains(text, StringComparison.OrdinalIgnoreCase)
        || (record.StructuredState?.Any(p => p.Value?.Contains(text, StringComparison.OrdinalIgnoreCase) == true) ?? false)
        || record.Scopes.Any(s => s?.ToString()?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
        || (record.Exception?.ToString().Contains(text, StringComparison.OrdinalIgnoreCase) ?? false);
}
