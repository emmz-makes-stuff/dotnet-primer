using System.Collections.Concurrent;

namespace SecureOrders;

/// <summary>A line of an order: a catalogue SKU and how many.</summary>
public sealed record OrderLine(string Sku, int Quantity);

/// <summary>Where an order is in its life.</summary>
public enum OrderStatus
{
    Placed,
    Refunded,
}

/// <summary>
/// A placed order. <see cref="CustomerId"/> is the <c>sub</c> claim of whoever placed it: that's
/// what the order's owner check compares against.
/// </summary>
public sealed record Order(int Id, string CustomerId, IReadOnlyList<OrderLine> Lines, long TotalCents, OrderStatus Status);

/// <summary>The body of <c>POST /orders</c>. The customer comes from the caller's token, not the body.</summary>
public sealed record PlaceOrderRequest(IReadOnlyList<OrderLine>? Lines);

/// <summary>The SKUs on sale, with their prices in cents.</summary>
public static class Catalogue
{
    public static readonly IReadOnlyDictionary<string, long> PricesCents = new Dictionary<string, long>
    {
        ["SKU-1"] = 1_250,
        ["SKU-2"] = 500,
        ["SKU-3"] = 9_900,
    };
}

/// <summary>The app's orders, in memory. Registered as a singleton, so it's thread-safe.</summary>
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, Order> _orders = new();
    private int _lastId;

    public Order Add(string customerId, IReadOnlyList<OrderLine> lines)
    {
        var total = lines.Sum(l => Catalogue.PricesCents[l.Sku] * l.Quantity);
        var order = new Order(Interlocked.Increment(ref _lastId), customerId, lines, total, OrderStatus.Placed);
        _orders[order.Id] = order;
        return order;
    }

    public Order? Find(int id) => _orders.GetValueOrDefault(id);

    public void Update(Order order) => _orders[order.Id] = order;
}

/// <summary>Checks a <see cref="PlaceOrderRequest"/> before anything is stored.</summary>
public static class OrderValidation
{
    public const int MaxLines = 50;
    public const int MaxQuantity = 1_000;

    /// <summary>What's wrong with the request, as messages for a validation problem; empty if it's valid.</summary>
    public static Dictionary<string, string[]> Problems(this PlaceOrderRequest request)
    {
        var problems = new Dictionary<string, string[]>();
        if (request.Lines is not { Count: > 0 and <= MaxLines } lines)
        {
            problems["lines"] = [$"An order needs from 1 to {MaxLines} lines."];
            return problems;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            // JSON can send null for an element even though the type says it can't be.
            if (lines[i] is not { } line)
            {
                problems[$"lines[{i}]"] = ["The line is required."];
            }
            else if (line.Sku is null || !Catalogue.PricesCents.ContainsKey(line.Sku))
            {
                problems[$"lines[{i}].sku"] = ["The SKU isn't in the catalogue."];
            }
            else if (line.Quantity is < 1 or > MaxQuantity)
            {
                problems[$"lines[{i}].quantity"] = [$"The quantity must be from 1 to {MaxQuantity}."];
            }
        }

        return problems;
    }
}
