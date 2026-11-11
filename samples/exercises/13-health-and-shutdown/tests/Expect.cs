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

    /// <summary>Starts an order, and waits until the store says it's in progress.</summary>
    public static async Task<Task<HttpResponseMessage>> OrderInProgressAsync(HealthOrdersApp app, HttpClient client)
    {
        var order = client.PostAsJsonAsync("/orders", new PlaceOrderRequest("SKU-1"), TestContext.Current.CancellationToken);
        try
        {
            await app.Store.OrderInProgress.WaitAsync(Patience, TestContext.Current.CancellationToken);
        }
        catch (TimeoutException)
        {
            var response = order.IsCompleted ? $" It answered {(int)(await order).StatusCode}." : "";
            Assert.Fail($"POST /orders didn't start an order within {Patience.TotalSeconds} s.{response}");
        }

        return order;
    }
}
