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

/// <summary>
/// TODO 3 (solved): unhealthy once the app has been told to stop, so the load balancer stops sending
/// it new requests while it finishes the ones it has.
/// </summary>
public sealed class ShuttingDownHealthCheck(IHostApplicationLifetime lifetime) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(lifetime.ApplicationStopping.IsCancellationRequested
            ? HealthCheckResult.Unhealthy("The app is shutting down.")
            : HealthCheckResult.Healthy());
}

/// <summary>
/// TODO 3 (solved): keeps the server answering for a moment after the stop signal, so that the
/// load balancer sees readiness fail before the app stops taking connections. Without it, Kestrel
/// refuses new connections at once, and nobody ever sees the shutting-down check.
/// <c>StoppingAsync</c> runs before any hosted service's <c>StopAsync</c>, so before the server
/// stops. A delay in <c>StopAsync</c> would come too late.
/// </summary>
public sealed partial class DrainDelay(ILogger<DrainDelay> logger) : IHostedLifecycleService
{
    /// <summary>
    /// In production, make this a few of the load balancer's health check intervals. It's short
    /// here, so that the tests are quick.
    /// </summary>
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(2);

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        LogDraining(Duration);
        try
        {
            await Task.Delay(Duration, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The shutdown timeout ran out first. Stop waiting, but don't throw: an exception here
            // fails the host's StopAsync, and the app exits with an error.
            LogDrainCutShort();
        }
    }

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Draining for {Delay} before the server stops")]
    private partial void LogDraining(TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The shutdown timeout ran out during the drain delay")]
    private partial void LogDrainCutShort();
}
