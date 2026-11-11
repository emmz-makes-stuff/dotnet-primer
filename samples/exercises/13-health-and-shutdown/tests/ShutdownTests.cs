using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;

namespace HealthOrders.Tests;

/// <summary>
/// Shared tests for exercise 13's shutdown. Each test starts an order, waits until the store says
/// it's in progress, and then tells the app to stop, as a SIGTERM does.
/// </summary>
public sealed class ShutdownTests
{
    /// <summary>
    /// How long readiness has to keep failing, with the server still answering, after the stop.
    /// This is the one timed check, because the drain delay is itself a length of time. Without a
    /// delay, Kestrel refuses connections within a fraction of a second; the Solution's delay is 2 s.
    /// </summary>
    private static readonly TimeSpan _drainWindow = TimeSpan.FromSeconds(1);

    [Fact]
    public async Task ReadinessFailsWhileShuttingDown()
    {
        using var app = new HealthOrdersApp();
        using var client = app.CreateClient();

        using (var ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken))
        {
            if (ready.StatusCode == HttpStatusCode.NotFound)
            {
                Assert.Fail("GET /health/ready returned 404, so there's no readiness to fail during shutdown. "
                    + "Do TODO 2 first, then TODO 3.");
            }
        }

        // Liveness's answer during shutdown is checked only once it ignores the store (TODO 1), so that
        // this test fails for TODO 3 alone.
        app.Store.Online = false;
        bool liveRunsNoChecks;
        using (var live = await client.GetAsync("/health/live", TestContext.Current.CancellationToken))
        {
            liveRunsNoChecks = live.StatusCode == HttpStatusCode.OK;
        }

        app.Store.Online = true;

        var order = await Expect.OrderInProgressAsync(app, client);
        var stopped = app.BeginStop();

        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < _drainWindow)
        {
            HttpResponseMessage ready;
            try
            {
                ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
            }
            catch (HttpRequestException e)
            {
                Assert.Fail($"{clock.Elapsed.TotalSeconds:F1} s after the stop, GET /health/ready failed ({e.HttpRequestError}): "
                    + "the server stopped taking connections at once, so no load balancer could see readiness fail. "
                    + "Keep the app answering for a moment after the stop, with a drain delay in an "
                    + "IHostedLifecycleService's StoppingAsync (TODO 3).");
                throw;
            }

            using (ready)
            {
                Expect.Status(ready, HttpStatusCode.ServiceUnavailable,
                    "the app is shutting down, but readiness still says it can take traffic. Add a check tagged \"ready\" "
                    + "that's unhealthy once IHostApplicationLifetime.ApplicationStopping has fired (TODO 3).");
            }

            if (liveRunsNoChecks)
            {
                using var live = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
                Expect.Status(live, HttpStatusCode.OK,
                    "liveness failed while the app was shutting down. It should still run no checks: a restart "
                    + "now would cut off the order in flight (TODO 3's check must be tagged \"ready\").");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        }

        // Let the order and the stop finish, whatever happens to them: that's the next test's job.
        try
        {
            using var response = await order.WaitAsync(Expect.Patience, TestContext.Current.CancellationToken);
        }
        catch (HttpRequestException)
        {
        }

        await stopped.WaitAsync(Expect.Patience, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InFlightOrderCompletesOnShutdown()
    {
        using var app = new HealthOrdersApp();
        using var client = app.CreateClient();

        var order = await Expect.OrderInProgressAsync(app, client);
        var stopped = app.BeginStop();

        HttpResponseMessage response;
        try
        {
            response = await order.WaitAsync(Expect.Patience, TestContext.Current.CancellationToken);
        }
        catch (HttpRequestException)
        {
            Assert.Fail("the order in flight was cut off when the app stopped: its connection was dropped. The shutdown timeout "
                + "is shorter than an order takes, so the host gave up waiting for it. Set "
                + "HostOptions.ShutdownTimeout above the longest request, and below the platform's stop timeout (TODO 4).");
            throw;
        }

        using (response)
        {
            Expect.Status(response, HttpStatusCode.Created, "the order in flight should complete during shutdown (TODO 4).");
            var placed = await response.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
            Assert.True(app.Store.Find(placed!.Id) is not null,
                $"order {placed.Id} was answered, but it isn't in the store (TODO 4).");
        }

        await stopped.WaitAsync(Expect.Patience, TestContext.Current.CancellationToken);
    }
}
