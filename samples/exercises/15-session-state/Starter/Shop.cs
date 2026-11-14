using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace SessionShop;

/// <summary>One line in a basket or an order.</summary>
public sealed record BasketLine(string Sku, int Quantity);

/// <summary>The body of <c>POST /basket/items</c>.</summary>
public sealed record AddItemRequest(string? Sku, int Quantity);

/// <summary>What <c>POST /checkout</c> returns.</summary>
public sealed record CheckoutResult(int OrderId, decimal Total);

/// <summary>A placed order.</summary>
public sealed record Order(int Id, IReadOnlyList<BasketLine> Lines, decimal Total);

/// <summary>A shopper's basket. Adding a SKU that's already in it adds to its quantity.</summary>
public sealed class Basket
{
    public List<BasketLine> Lines { get; init; } = [];

    public decimal Total => PriceList.Total(Lines);

    public void Add(string sku, int quantity)
    {
        var index = Lines.FindIndex(line => line.Sku == sku);
        if (index >= 0)
        {
            Lines[index] = Lines[index] with { Quantity = Lines[index].Quantity + quantity };
        }
        else
        {
            Lines.Add(new BasketLine(sku, quantity));
        }
    }
}

/// <summary>The shop's prices.</summary>
public static class PriceList
{
    private static readonly FrozenDictionary<string, decimal> _prices = new Dictionary<string, decimal>
    {
        ["SKU-1"] = 4.50m,
        ["SKU-2"] = 12.00m,
        ["SKU-3"] = 0.99m,
    }.ToFrozenDictionary();

    public static bool Sells(string sku) => _prices.ContainsKey(sku);

    public static decimal Total(IEnumerable<BasketLine> lines) => lines.Sum(line => _prices[line.Sku] * line.Quantity);
}

/// <summary>
/// The shop's orders, in memory. It stands in for a database: the place for data the shop must
/// keep, whichever server handled the checkout.
/// </summary>
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, Order> _orders = new();
    private int _lastId;

    public Order Place(IReadOnlyList<BasketLine> lines)
    {
        var order = new Order(Interlocked.Increment(ref _lastId), [.. lines], PriceList.Total(lines));
        _orders[order.Id] = order;
        return order;
    }

    public Order? Find(int id) => _orders.GetValueOrDefault(id);
}
