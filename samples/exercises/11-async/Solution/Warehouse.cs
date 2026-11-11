namespace AsyncOrders;

/// <summary>
/// The warehouse's API: a slow dependency, the kind of call that waits on a network or a database.
/// Every method takes a <see cref="CancellationToken"/>, so a caller that gives up can stop the work.
/// The tests swap in a fake they control.
/// </summary>
public interface IWarehouse
{
    /// <summary>Holds stock for the order's lines.</summary>
    Task ReserveAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken);

    /// <summary>Whether every line is in stock now.</summary>
    Task<bool> GetStockAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken);

    /// <summary>The lines' price, in cents.</summary>
    Task<long> GetPriceAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken);

    /// <summary>What shipping the lines costs, in cents.</summary>
    Task<long> GetShippingAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken);

    /// <summary>Tells the warehouse that an order was placed, so it can pick and pack.</summary>
    Task NotifyAsync(Order order, CancellationToken cancellationToken);
}

/// <summary>
/// A pretend warehouse: each call waits half a second, the way a remote call would, and gives up
/// when its token is cancelled. Waiting with <see cref="Task.Delay(TimeSpan, CancellationToken)"/>
/// holds no thread.
/// </summary>
public sealed class SlowWarehouse : IWarehouse
{
    private static readonly TimeSpan _latency = TimeSpan.FromMilliseconds(500);

    private static readonly Dictionary<string, long> _pricesCents = new()
    {
        ["SKU-1"] = 1_250,
        ["SKU-2"] = 500,
        ["SKU-3"] = 9_900,
    };

    public Task ReserveAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken) =>
        Task.Delay(_latency, cancellationToken);

    public async Task<bool> GetStockAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken)
    {
        await Task.Delay(_latency, cancellationToken);
        return lines.All(l => l.Quantity <= 100);
    }

    public async Task<long> GetPriceAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken)
    {
        await Task.Delay(_latency, cancellationToken);
        return lines.Sum(l => _pricesCents.GetValueOrDefault(l.Sku) * l.Quantity);
    }

    public async Task<long> GetShippingAsync(IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken)
    {
        await Task.Delay(_latency, cancellationToken);
        return 499 + (100L * lines.Sum(l => l.Quantity));
    }

    public Task NotifyAsync(Order order, CancellationToken cancellationToken) =>
        Task.Delay(_latency, cancellationToken);
}
