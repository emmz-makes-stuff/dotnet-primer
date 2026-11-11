using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthOrders;

/// <summary>Unhealthy while the store is offline: the app can't take orders, so it isn't ready.</summary>
public sealed class StoreHealthCheck(OrderStore store) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Online
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The order store is offline."));
}

// TODO 3: When the app is told to stop, readiness should fail, so that the load balancer stops
//   sending new requests while the app finishes the ones it has. Two parts, both here:
//   - a health check that's unhealthy once IHostApplicationLifetime.ApplicationStopping has fired.
//     Register it in Program.cs, tagged "ready";
//   - a drain delay: Kestrel refuses new connections as soon as the host stops, so nobody would see
//     that check fail. Write an IHostedLifecycleService that waits a couple of seconds in
//     StoppingAsync (which runs before any hosted service's StopAsync, and so before the server
//     stops), and register it with AddHostedService. A delay in StopAsync comes too late. If the
//     shutdown timeout runs out during the delay, StoppingAsync's token is cancelled: stop waiting,
//     but don't let the exception out, or the host's StopAsync fails with it.
