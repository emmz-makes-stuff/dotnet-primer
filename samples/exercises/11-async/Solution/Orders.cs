using System.Collections.Concurrent;

namespace AsyncOrders;

/// <summary>A line of an order: a catalogue SKU and how many.</summary>
public sealed record OrderLine(string Sku, int Quantity);

/// <summary>A placed order.</summary>
public sealed record Order(int Id, IReadOnlyList<OrderLine> Lines);

/// <summary>The body of <c>POST /orders</c>.</summary>
public sealed record PlaceOrderRequest(IReadOnlyList<OrderLine>? Lines);

/// <summary>What <c>GET /orders/{id}/quote</c> answers: three figures from the warehouse.</summary>
public sealed record Quote(int OrderId, bool InStock, long PriceCents, long ShippingCents);

/// <summary>The SKUs the warehouse stocks.</summary>
public static class Catalogue
{
    public static readonly IReadOnlySet<string> Skus = new HashSet<string> { "SKU-1", "SKU-2", "SKU-3" };
}

/// <summary>The app's orders, in memory. Registered as a singleton, so it's thread-safe.</summary>
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, Order> _orders = new();
    private int _lastId;

    public Order Add(IReadOnlyList<OrderLine> lines)
    {
        var order = new Order(Interlocked.Increment(ref _lastId), lines);
        _orders[order.Id] = order;
        return order;
    }

    public Order? Find(int id) => _orders.GetValueOrDefault(id);
}

/// <summary>Checks a <see cref="PlaceOrderRequest"/> before anything is reserved or stored.</summary>
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
            else if (line.Sku is null || !Catalogue.Skus.Contains(line.Sku))
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
