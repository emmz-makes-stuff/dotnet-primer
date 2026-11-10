using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using GrpcOrders.Protos;

namespace GrpcOrders;

/// <summary>An order, as the store keeps it. Money is in cents, as on the wire.</summary>
public sealed record StoredOrder(int Id, string CustomerEmail, IReadOnlyList<StoredLine> Lines, DateTimeOffset PlacedAt)
{
    public long TotalCents => Lines.Sum(l => l.Quantity * l.UnitPriceCents);
}

/// <summary>One line of a <see cref="StoredOrder"/>.</summary>
public sealed record StoredLine(string Sku, int Quantity, long UnitPriceCents);

/// <summary>
/// The orders, in memory. It stands in for a database (exercise 07 has the real one), so this
/// exercise needs no Docker. Registered as a singleton, so it's thread-safe.
/// </summary>
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, StoredOrder> _orders = new();
    private int _lastId;

    /// <summary>How many orders are stored.</summary>
    public int Count => _orders.Count;

    /// <summary>Stores a new order, placed now, and returns it with its id.</summary>
    public StoredOrder Add(string customerEmail, IReadOnlyList<StoredLine> lines)
    {
        var order = new StoredOrder(Interlocked.Increment(ref _lastId), customerEmail, lines, DateTimeOffset.UtcNow);
        _orders[order.Id] = order;
        return order;
    }

    /// <summary>The order with this id, or null if there isn't one.</summary>
    public StoredOrder? Find(int id) => _orders.GetValueOrDefault(id);
}

/// <summary>Checks a <see cref="PlaceOrderRequest"/> before it's stored.</summary>
public static class OrderValidation
{
    /// <summary>The most of one SKU a line can order.</summary>
    public const int MaxQuantity = 1_000;

    /// <summary>
    /// The highest unit price, in cents ($100,000). With <see cref="MaxQuantity"/>, it keeps an
    /// order's total far from overflowing a long.
    /// </summary>
    public const long MaxUnitPriceCents = 10_000_000;

    /// <summary>What's wrong with the request: empty if it can be stored as it is.</summary>
    /// <remarks>
    /// Proto3 has no null for strings or repeated fields: a field the caller didn't set arrives as
    /// "" or an empty list. So "missing" and "empty" are the same check.
    /// </remarks>
    public static List<string> Problems(this PlaceOrderRequest request)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            problems.Add("customer_email is required.");
        }
        else if (request.CustomerEmail.Length > 256)
        {
            problems.Add("customer_email can be at most 256 characters.");
        }

        if (request.Lines.Count == 0)
        {
            problems.Add("An order needs at least one line.");
        }

        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            if (string.IsNullOrWhiteSpace(line.Sku))
            {
                problems.Add($"lines[{i}].sku is required.");
            }
            else if (line.Sku.Length > 32)
            {
                problems.Add($"lines[{i}].sku can be at most 32 characters.");
            }

            if (line.Quantity < 1)
            {
                problems.Add($"lines[{i}].quantity must be at least 1.");
            }
            else if (line.Quantity > MaxQuantity)
            {
                problems.Add($"lines[{i}].quantity can be at most {MaxQuantity}.");
            }

            if (line.UnitPriceCents < 0)
            {
                problems.Add($"lines[{i}].unit_price_cents can't be negative.");
            }
            else if (line.UnitPriceCents > MaxUnitPriceCents)
            {
                problems.Add($"lines[{i}].unit_price_cents can be at most {MaxUnitPriceCents}.");
            }
        }

        return problems;
    }
}

/// <summary>Where an order is in the warehouse, as it changes.</summary>
public interface IOrderTracker
{
    /// <summary>
    /// The order's status updates, as they happen. It stops early, by throwing
    /// <see cref="OperationCanceledException"/>, when <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    IAsyncEnumerable<OrderStatus> TrackAsync(int orderId, CancellationToken cancellationToken);
}

/// <summary>
/// A pretend warehouse: an order is placed, picked, packed and shipped, a second apart. Each wait
/// takes the cancellation token, so the tracking stops as soon as the caller gives up.
/// </summary>
public sealed class SimulatedOrderTracker : IOrderTracker
{
    private static readonly TimeSpan _step = TimeSpan.FromSeconds(1);

    public async IAsyncEnumerable<OrderStatus> TrackAsync(int orderId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        OrderStatus[] statuses = [OrderStatus.Placed, OrderStatus.Picked, OrderStatus.Packed, OrderStatus.Shipped];
        for (var i = 0; i < statuses.Length; i++)
        {
            if (i > 0)
            {
                await Task.Delay(_step, cancellationToken);
            }

            yield return statuses[i];
        }
    }
}
