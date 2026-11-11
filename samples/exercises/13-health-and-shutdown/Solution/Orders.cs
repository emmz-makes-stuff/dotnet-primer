using System.Collections.Concurrent;

namespace HealthOrders;

/// <summary>A placed order.</summary>
public sealed record Order(int Id, string Sku);

/// <summary>The body of <c>POST /orders</c>.</summary>
public sealed record PlaceOrderRequest(string? Sku);

/// <summary>
/// The app's orders, in memory. It stands in for a database: the tests take it offline, and it
/// signals when an order is in progress, so a test can stop the app at exactly that moment.
/// </summary>
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, Order> _orders = new();
    private readonly TaskCompletionSource _orderInProgress = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _lastId;

    /// <summary>Whether the store can be reached. The readiness check reports on this.</summary>
    public bool Online { get; set; } = true;

    /// <summary>Completes when the first order has started, and is waiting on its payment.</summary>
    public Task OrderInProgress => _orderInProgress.Task;

    /// <summary>Called when an order starts.</summary>
    public void Begin() => _orderInProgress.TrySetResult();

    public Order Add(string sku)
    {
        var order = new Order(Interlocked.Increment(ref _lastId), sku);
        _orders[order.Id] = order;
        return order;
    }

    public Order? Find(int id) => _orders.GetValueOrDefault(id);
}

/// <summary>A payment provider that takes a few seconds, as a real one can.</summary>
public sealed class Payments
{
    /// <summary>How long a payment takes. Fixed: the exercise doesn't change it.</summary>
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(3);

    /// <summary>Waits for the payment, or for the caller to give up.</summary>
    public Task TakeAsync(CancellationToken cancellationToken) => Task.Delay(Duration, cancellationToken);
}
