using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Orders;

namespace Orders.Tests;

/// <summary>
/// Shared tests for exercise 05. Compiled into both Starter.Tests and
/// Solution.Tests, so they run against whichever app the project references.
/// Against the starter they fail until its TODOs are done.
/// </summary>
/// <remarks>
/// An <see cref="ActivityListener"/> sees spans from the whole process, so each test sends its own
/// <c>traceparent</c> header and looks only at spans in that trace. The tests also share one
/// collection, so they never run in parallel with each other.
/// </remarks>
[Collection(nameof(OrderTracingTests))]
public sealed class OrderTracingTests(OrderTracingTests.App app) : IClassFixture<OrderTracingTests.App>
{
    /// <summary>A customer email that makes the fake payment gateway fail.</summary>
    internal const string FailingEmail = "sam.jones@fail.test";

    /// <summary>The app, with every finished span from the two sources recorded in memory.</summary>
    public sealed class App : WebApplicationFactory<Program>
    {
        private readonly ActivityListener _listener;

        public App()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name is "Orders" or "Microsoft.AspNetCore",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = Spans.Add,
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public ConcurrentBag<Activity> Spans { get; } = [];

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listener.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    [Fact]
    public async Task OrderSpanJoinsTheRequestTrace()
    {
        var (traceId, orderId) = await PlaceOrderAsync("jo.bloggs@example.test", HttpStatusCode.Created, itemCount: 3);

        var request = await RequestSpanAsync(traceId);
        var placeOrder = SpanNamed(traceId, "PlaceOrder");
        Assert.True(
            placeOrder is not null,
            "Expected a span named PlaceOrder in the request's trace, but found none. Create an ActivitySource " +
            "named \"Orders\" and start a PlaceOrder activity in the endpoint (and dispose it when the order is done).");
        Assert.True(
            placeOrder.ParentSpanId == request.SpanId,
            "The PlaceOrder span should be a child of the request's span. Start it while handling the request, " +
            "with StartActivity, which parents it on the current span.");
        Assert.True(
            placeOrder.GetTagItem("order.id") as string == orderId,
            $"The PlaceOrder span should have an order.id attribute of {orderId}, but it was \"{placeOrder.GetTagItem("order.id")}\".");
        Assert.True(
            placeOrder.GetTagItem("order.item_count")?.ToString() == "3",
            $"The PlaceOrder span should have an order.item_count attribute of 3, but it was \"{placeOrder.GetTagItem("order.item_count")}\".");
    }

    [Fact]
    public async Task PaymentStepHasItsOwnSpan()
    {
        var (traceId, _) = await PlaceOrderAsync("jo.bloggs@example.test", HttpStatusCode.Created);

        await RequestSpanAsync(traceId);
        var placeOrder = SpanNamed(traceId, "PlaceOrder");
        var chargePayment = SpanNamed(traceId, "ChargePayment");
        Assert.True(
            chargePayment is not null,
            "Expected a span named ChargePayment in the request's trace, but found none. Start one around the " +
            "payments.Charge call.");
        Assert.True(
            placeOrder is not null && chargePayment.ParentSpanId == placeOrder.SpanId,
            "The ChargePayment span should be a child of the PlaceOrder span. Start it while PlaceOrder is still open.");
    }

    [Fact]
    public async Task FailedPaymentMarksTheSpan()
    {
        var (traceId, _) = await PlaceOrderAsync(FailingEmail, HttpStatusCode.BadGateway);

        await RequestSpanAsync(traceId);
        var chargePayment = SpanNamed(traceId, "ChargePayment");
        Assert.True(
            chargePayment is not null,
            "Expected a span named ChargePayment in the request's trace, but found none.");
        Assert.True(
            chargePayment.Status == ActivityStatusCode.Error,
            $"When the gateway throws, the ChargePayment span's status should be Error, but it was {chargePayment.Status}. " +
            "Call SetStatus(ActivityStatusCode.Error, …) in the catch block.");

        var exception = chargePayment.Events.FirstOrDefault(e => e.Name == "exception");
        Assert.True(
            exception.Name == "exception",
            "The ChargePayment span should carry an \"exception\" event. Call AddException(ex) in the catch block.");
        var tags = exception.Tags.ToDictionary(t => t.Key, t => t.Value?.ToString());
        Assert.True(
            tags.GetValueOrDefault("exception.type")?.EndsWith(nameof(PaymentGatewayException), StringComparison.Ordinal) == true,
            $"The exception event's exception.type should be the PaymentGatewayException, but it was \"{tags.GetValueOrDefault("exception.type")}\".");
        Assert.True(
            tags.GetValueOrDefault("exception.message") == "The payment gateway timed out.",
            $"The exception event's exception.message should be the exception's message, but it was \"{tags.GetValueOrDefault("exception.message")}\".");
    }

    /// <summary>Places an order inside a new trace of the test's own, and returns that trace's id.</summary>
    private async Task<(ActivityTraceId TraceId, string OrderId)> PlaceOrderAsync(
        string email, HttpStatusCode expected, int itemCount = 1)
    {
        var traceId = ActivityTraceId.CreateRandom();
        using var client = app.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/orders")
        {
            Content = JsonContent.Create(new PlaceOrderRequest(
                email,
                "card",
                [.. Enumerable.Range(1, itemCount).Select(i => new OrderItem($"SKU-{i}", 1))],
                42.50m)),
        };
        // W3C trace context: version-traceid-parentspanid-flags. ASP.NET Core joins this trace.
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.True(
            response.StatusCode == expected,
            $"POST /orders should return {(int)expected}, but returned {(int)response.StatusCode}.");
        var body = await response.Content.ReadFromJsonAsync<PlaceOrderResponse>(TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrEmpty(body?.OrderId), "The response should include the order id.");
        return (traceId, body!.OrderId);
    }

    /// <summary>
    /// The request's own span. ASP.NET Core can stop it just after the response reaches the client,
    /// so wait briefly for it.
    /// </summary>
    private async Task<Activity> RequestSpanAsync(ActivityTraceId traceId)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var request = app.Spans.FirstOrDefault(s => s.TraceId == traceId && s.Source.Name == "Microsoft.AspNetCore");
            if (request is not null)
            {
                return request;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.Fail("The request's span was never recorded, so the app didn't join the test's trace.");
        return null!;
    }

    private Activity? SpanNamed(ActivityTraceId traceId, string name) =>
        app.Spans.FirstOrDefault(s => s.TraceId == traceId && s.Source.Name == "Orders" && s.DisplayName == name);
}
