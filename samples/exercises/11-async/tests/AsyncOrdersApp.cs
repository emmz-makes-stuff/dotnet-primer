using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace AsyncOrders.Tests;

/// <summary>
/// The app under test, in memory, with a <see cref="FakeWarehouse"/> the tests control, and every
/// log entry captured. Each test makes its own, so no fake's state leaks into another test.
/// </summary>
public sealed class AsyncOrdersApp : WebApplicationFactory<Program>
{
    public FakeWarehouse Warehouse { get; } = new();

    public FakeLogCollector Logs => Services.GetFakeLogCollector();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.AddFakeLogging());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWarehouse>();
            services.AddSingleton<IWarehouse>(Warehouse);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // First, so that a call the fake is holding ends, and the app can stop.
            Warehouse.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// A warehouse that answers at once, unless a test asks it to wait. Nothing it does depends on how
/// fast the machine is: it waits for events, and its timeouts only bound how long a failing test
/// takes to report.
/// </summary>
public sealed class FakeWarehouse : IWarehouse, IDisposable
{
    public const long PriceCents = 1_000;
    public const long ShippingCents = 500;

    /// <summary>How long a quote call waits for the other two to start, before it gives up.</summary>
    public static readonly TimeSpan QuoteTimeout = TimeSpan.FromSeconds(5);

    // The three quote calls meet here: it opens only when all three are in flight at once.
    private readonly Barrier _quoteCalls = new(3);

    // Cancelled on Dispose, to end a reservation that its own token never cancels.
    private readonly CancellationTokenSource _released = new();
    private int _disposed;

    /// <summary>When set, <see cref="ReserveAsync"/> waits until its token is cancelled.</summary>
    public bool HoldReservations { get; set; }

    /// <summary>When set, <see cref="NotifyAsync"/> fails.</summary>
    public bool FailNotifications { get; set; }

    /// <summary>Completes when a held reservation has started.</summary>
    public TaskCompletionSource ReserveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes when a held reservation saw its token cancelled, and stopped.</summary>
    public TaskCompletionSource ReserveCancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task ReserveAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken)
    {
        if (!HoldReservations)
        {
            return;
        }

        ReserveStarted.TrySetResult();
        using var either = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _released.Token);
        try
        {
            await Task.Delay(Timeout.Infinite, either.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ReserveCancelled.TrySetResult();
            throw;
        }
    }

    public async Task<bool> GetStockAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken)
    {
        await MeetTheOtherQuoteCallsAsync(cancellationToken);
        return true;
    }

    public async Task<long> GetPriceAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken)
    {
        await MeetTheOtherQuoteCallsAsync(cancellationToken);
        return PriceCents;
    }

    public async Task<long> GetShippingAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken)
    {
        await MeetTheOtherQuoteCallsAsync(cancellationToken);
        return ShippingCents;
    }

    public Task NotifyAsync(Order order, CancellationToken cancellationToken) =>
        FailNotifications
            ? Task.FromException(new InvalidOperationException($"The warehouse is down: order {order.Id} wasn't sent."))
            : Task.CompletedTask;

    // WebApplicationFactory's DisposeAsync calls Dispose(true) again, so this runs twice.
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _released.Cancel();
        _released.Dispose();
        _quoteCalls.Dispose();
    }

    // Waits on a thread-pool thread, not the request's: the barrier is a test device, and blocking
    // the caller would hide the very thing the test looks for.
    private Task MeetTheOtherQuoteCallsAsync(CancellationToken cancellationToken) => Task.Run(() =>
    {
        if (!_quoteCalls.SignalAndWait(QuoteTimeout, cancellationToken))
        {
            throw new TimeoutException($"This quote call waited {QuoteTimeout.TotalSeconds} s for the other two, and they never started.");
        }
    }, cancellationToken);
}
