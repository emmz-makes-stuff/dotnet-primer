using System.Collections.Concurrent;

namespace ContainerOrders;

/// <summary>A placed order: a catalogue SKU and how many.</summary>
public sealed record Order(int Id, string Sku, int Quantity);

/// <summary>The body of <c>POST /orders</c>.</summary>
public sealed record PlaceOrderRequest(string? Sku, int Quantity);

/// <summary>The app's orders, in memory. Registered as a singleton, so it's thread-safe.</summary>
/// <remarks>
/// It's in memory so the exercise needs no database: this exercise is about the image, not the data.
/// </remarks>
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, Order> _orders = new();
    private int _lastId;

    public Order Add(string sku, int quantity)
    {
        var order = new Order(Interlocked.Increment(ref _lastId), sku, quantity);
        _orders[order.Id] = order;
        return order;
    }

    public IReadOnlyList<Order> All() => [.. _orders.Values.OrderBy(order => order.Id)];
}
