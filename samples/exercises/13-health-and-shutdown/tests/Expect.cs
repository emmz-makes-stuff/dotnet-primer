using System.Net;
using System.Net.Http.Json;

namespace HealthOrders.Tests;

public static class Expect
{
    /// <summary>How long a test waits for something the app should do, before it reports a failure.</summary>
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    public static void Status(HttpResponseMessage response, HttpStatusCode expected, string hint)
    {
        if (response.StatusCode != expected)
        {
            var request = response.RequestMessage!;
            Assert.Fail(
                $"{request.Method} {request.RequestUri!.PathAndQuery} returned {(int)response.StatusCode} {response.StatusCode}, "
                + $"expected {(int)expected} {expected}: {hint}");
        }
    }

    /// <summary>
    /// Starts an order, and waits until the store says it's in progress. A POST that finishes
    /// without starting one, or fails, is reported straight away, with its status or its error.
    /// </summary>
    public static async Task<Task<HttpResponseMessage>> OrderInProgressAsync(HealthOrdersApp app, HttpClient client)
    {
        var order = client.PostAsJsonAsync("/orders", new PlaceOrderRequest("SKU-1"), TestContext.Current.CancellationToken);
        var inProgress = app.Store.OrderInProgress.WaitAsync(Patience, TestContext.Current.CancellationToken);
        await Task.WhenAny(inProgress, order);

        if (inProgress.IsCompletedSuccessfully)
        {
            return order;
        }

        if (order.IsCompletedSuccessfully)
        {
            using var response = await order;
            Assert.Fail($"POST /orders answered {(int)response.StatusCode} {response.StatusCode} without starting an order.");
        }

        if (order.IsFaulted)
        {
            Assert.Fail($"POST /orders failed without starting an order: {order.Exception!.GetBaseException().Message}");
        }

        try
        {
            await inProgress;
        }
        catch (TimeoutException)
        {
            Assert.Fail($"POST /orders didn't start an order within {Patience.TotalSeconds} s.");
        }

        return order;
    }
}
