using System.Net;
using System.Net.Http.Json;

namespace HealthOrders.Tests;

/// <summary>
/// Shared tests for exercise 13's health endpoints. Compiled into both Starter.Tests and
/// Solution.Tests, so they run against whichever app the project references.
/// </summary>
public sealed class HealthTests
{
    [Fact]
    public async Task PlacedOrderCanBeFetched()
    {
        using var app = new HealthOrdersApp();
        using var client = app.CreateClient();

        using var placed = await client.PostAsJsonAsync("/orders", new PlaceOrderRequest("SKU-1"), TestContext.Current.CancellationToken);
        Expect.Status(placed, HttpStatusCode.Created, "the order wasn't placed.");
        var order = await placed.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);

        using var fetched = await client.GetAsync($"/orders/{order!.Id}", TestContext.Current.CancellationToken);
        Expect.Status(fetched, HttpStatusCode.OK, "the order that POST /orders created wasn't found.");
    }

    [Fact]
    public async Task LivenessIgnoresTheStore()
    {
        using var app = new HealthOrdersApp();
        using var client = app.CreateClient();
        app.Store.Online = false;

        using var live = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Expect.Status(live, HttpStatusCode.OK,
            "with the store offline, liveness failed. An orchestrator restarts an app whose liveness fails, and a "
            + "restart can't bring the store back, so the outage gets worse. Liveness should run no checks (TODO 1).");
    }

    [Fact]
    public async Task ReadinessFollowsTheStore()
    {
        using var app = new HealthOrdersApp();
        using var client = app.CreateClient();

        using var online = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        if (online.StatusCode == HttpStatusCode.NotFound)
        {
            Assert.Fail("GET /health/ready returned 404: there's no readiness endpoint. Map /health/ready to the "
                + "checks tagged \"ready\", and tag the store's check \"ready\" (TODO 2).");
        }

        Expect.Status(online, HttpStatusCode.OK, "with the store online, the app is ready (TODO 2).");

        app.Store.Online = false;
        using var offline = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        Expect.Status(offline, HttpStatusCode.ServiceUnavailable,
            "with the store offline, the app can't take orders, so readiness should fail. Tag the store's check "
            + "\"ready\", so that /health/ready runs it (TODO 2).");
    }
}
