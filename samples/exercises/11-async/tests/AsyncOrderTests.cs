using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AsyncOrders.Tests;

/// <summary>
/// Shared tests for exercise 11. Compiled into both Starter.Tests and Solution.Tests, so they run
/// against whichever app the project references. Against the starter, all but the first fail until
/// its TODOs are done, each naming its TODO.
/// No test times anything: each waits for an event, or reads the code.
/// </summary>
public sealed class AsyncOrderTests
{
    private static readonly PlaceOrderRequest _order = new([new("SKU-1", 2), new("SKU-3", 1)]);

    /// <summary>How long a test waits for something the app should do, before it reports a failure.</summary>
    private static readonly TimeSpan _patience = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task PlacedOrderCanBeFetched()
    {
        using var app = new AsyncOrdersApp();
        using var client = app.CreateClient();

        var id = await PlaceOrderAsync(client);
        using var response = await client.GetAsync($"/orders/{id}", TestContext.Current.CancellationToken);

        Expect.Status(response, HttpStatusCode.OK, "the order that POST /orders created wasn't found.");
    }

    [Fact]
    public void PlacingAnOrderHoldsNoThreadWhileItWaits()
    {
        using var app = new AsyncOrdersApp();
        var handler = Handler(app, "POST", "/orders");

        var problems = BlockingCalls.In(handler)
            .Select(call => $"POST /orders calls {call}: it holds a thread while the warehouse works. Await it instead (TODO 1).")
            .ToList();
        if (!typeof(Task).IsAssignableFrom(handler.ReturnType))
        {
            problems.Add($"POST /orders's handler returns {handler.ReturnType.Name}, not a Task, so it can't await "
                + "the warehouse, and can only wait by holding a thread. Make it async (TODO 1).");
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public async Task CancelledOrderStopsTheWarehouse()
    {
        using var app = new AsyncOrdersApp();
        app.Warehouse.HoldReservations = true;
        using var client = app.CreateClient();
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        // On another thread: TestServer runs the handler on the caller's thread until it first awaits,
        // so a handler that blocks would block this test too, and it could never cancel.
        var placing = Task.Run(() => client.PostAsJsonAsync("/orders", _order, caller.Token), TestContext.Current.CancellationToken);
        await Within(app.Warehouse.ReserveStarted.Task, "POST /orders never asked the warehouse to reserve stock.");

        // The caller gives up while the warehouse is still working.
        await caller.CancelAsync();
        await Within(app.Warehouse.ReserveCancelled.Task,
            "the caller cancelled POST /orders, but the warehouse's ReserveAsync never saw it, and kept working. "
            + "Take the endpoint's CancellationToken, and pass it to ReserveAsync (TODO 2).");

        // Only now: a handler that blocks holds the request until the app is disposed, so awaiting this
        // first would wait for ever.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => placing);
    }

    [Fact]
    public async Task FailedNotificationIsLogged()
    {
        using var app = new AsyncOrdersApp();
        app.Warehouse.FailNotifications = true;
        using var client = app.CreateClient();

        var id = await PlaceOrderAsync(client);
        var logged = await EventuallyAsync(() => app.Logs.GetSnapshot().Any(record =>
            record.Level == LogLevel.Error
            && record.StructuredState?.Any(p => p.Key == "OrderId" && p.Value == id.ToString(CultureInfo.InvariantCulture)) == true));

        Assert.True(logged,
            $"the warehouse notification for order {id} failed, and nothing logged it: the failure was lost. "
            + "Hand the order to OrderNotifications, whose worker awaits each notification and logs a failure (TODO 3).");

        // The failure didn't take the app down.
        using var response = await client.GetAsync($"/orders/{id}", TestContext.Current.CancellationToken);
        Expect.Status(response, HttpStatusCode.OK, "the app stopped answering after a notification failed.");
    }

    [Fact]
    public async Task QuoteAsksTheWarehouseConcurrently()
    {
        using var app = new AsyncOrdersApp();
        using var client = app.CreateClient();
        var id = await PlaceOrderAsync(client);

        // Each of the fake's quote calls waits until all three are in flight. One after another, the
        // first never sees the other two, and throws a TimeoutException after FakeWarehouse.QuoteTimeout.
        // TestServer hands an exception the app didn't handle to the client, so it arrives here.
        const string hint = "the quote's calls ran one after another, so the first waited for the others in vain. "
            + "Start all three, then await Task.WhenAll (TODO 4).";
        HttpResponseMessage received;
        try
        {
            received = await client.GetAsync($"/orders/{id}/quote", TestContext.Current.CancellationToken);
        }
        catch (TimeoutException)
        {
            Assert.Fail($"GET /orders/{id}/quote failed: {hint}");
            throw; // Not reached: Assert.Fail throws. The compiler needs it to see that `received` is set below.
        }

        using var response = received;
        Expect.Status(response, HttpStatusCode.OK, hint);
        var quote = await response.Content.ReadFromJsonAsync<Quote>(TestContext.Current.CancellationToken);
        Assert.Equal(new Quote(id, true, FakeWarehouse.PriceCents, FakeWarehouse.ShippingCents), quote);
    }

    private static async Task<int> PlaceOrderAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/orders", _order, TestContext.Current.CancellationToken);
        Expect.Status(response, HttpStatusCode.Created, "a valid order wasn't placed.");
        var order = await response.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        return order!.Id;
    }

    /// <summary>The handler method of the app's endpoint for this method and route.</summary>
    private static MethodInfo Handler(AsyncOrdersApp app, string httpMethod, string route)
    {
        var endpoint = app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .SingleOrDefault(e => e.RoutePattern.RawText == route
                && e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains(httpMethod) == true);
        Assert.True(endpoint is not null, $"The app has no {httpMethod} {route} endpoint.");

        // Minimal APIs put the handler's MethodInfo in the endpoint's metadata.
        var handler = endpoint.Metadata.GetMetadata<MethodInfo>();
        Assert.True(handler is not null, $"{httpMethod} {route} has no handler method in its metadata.");
        return handler;
    }

    /// <summary>Waits for <paramref name="task"/>, and fails with <paramref name="hint"/> if it takes longer than <see cref="_patience"/>.</summary>
    private static async Task Within(Task task, string hint)
    {
        try
        {
            await task.WaitAsync(_patience, TestContext.Current.CancellationToken);
        }
        catch (TimeoutException)
        {
            Assert.Fail($"Nothing happened within {_patience.TotalSeconds} s: {hint}");
        }
    }

    /// <summary>Whether <paramref name="condition"/> comes true within <see cref="_patience"/>.</summary>
    private static async Task<bool> EventuallyAsync(Func<bool> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(_patience);
        while (!condition())
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);
            }
            catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
            {
                return condition();
            }
        }

        return true;
    }
}

public static class Expect
{
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
}
