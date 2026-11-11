using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace TestingOrders;

/// <summary>One line of an order. Money is in cents.</summary>
public sealed record OrderLine(string Sku, int Quantity, long UnitPriceCents);

/// <summary>Where an order is.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))]
public enum OrderStatus
{
    Placed,
    Cancelled,
}

/// <summary>An order, as the store keeps it and the API returns it.</summary>
public sealed record Order(
    int Id, string CustomerEmail, IReadOnlyList<OrderLine> Lines, long TotalCents, DateTimeOffset PlacedAt, OrderStatus Status);

/// <summary>The body of <c>POST /orders</c>.</summary>
public sealed record PlaceOrderRequest(string? CustomerEmail, IReadOnlyList<OrderLine>? Lines);

/// <summary>The orders, in memory. Registered as a singleton, so it's thread-safe.</summary>
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, Order> _orders = new();
    private int _lastId;

    /// <summary>Stores a new order and returns it with its id.</summary>
    public Order Add(string customerEmail, IReadOnlyList<OrderLine> lines, long totalCents, DateTimeOffset placedAt)
    {
        var order = new Order(Interlocked.Increment(ref _lastId), customerEmail, lines, totalCents, placedAt, OrderStatus.Placed);
        _orders[order.Id] = order;
        return order;
    }

    /// <summary>The order with this id, or null if there isn't one.</summary>
    public Order? Find(int id) => _orders.GetValueOrDefault(id);

    /// <summary>Replaces a stored order with a changed copy of it.</summary>
    public void Update(Order order) => _orders[order.Id] = order;
}

/// <summary>Checks a <see cref="PlaceOrderRequest"/> before it's stored.</summary>
public static class OrderValidation
{
    /// <summary>The most of one SKU a line can order.</summary>
    public const int MaxQuantity = 1_000;

    /// <summary>The highest unit price, in cents ($100,000).</summary>
    public const long MaxUnitPriceCents = 10_000_000;

    /// <summary>What's wrong with the request: empty if it can be stored as it is.</summary>
    public static List<string> Problems(this PlaceOrderRequest request)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            problems.Add("customerEmail is required.");
        }
        else if (request.CustomerEmail.Length > 256)
        {
            problems.Add("customerEmail can be at most 256 characters.");
        }

        if (request.Lines is not { Count: > 0 } lines)
        {
            problems.Add("An order needs at least one line.");
            return problems;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            // JSON can send null for an element even though the type says it can't be.
            if (lines[i] is not { } line)
            {
                problems.Add($"lines[{i}] is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(line.Sku))
            {
                problems.Add($"lines[{i}].sku is required.");
            }

            if (line.Quantity is < 1 or > MaxQuantity)
            {
                problems.Add($"lines[{i}].quantity must be from 1 to {MaxQuantity}.");
            }

            if (line.UnitPriceCents is < 0 or > MaxUnitPriceCents)
            {
                problems.Add($"lines[{i}].unitPriceCents must be from 0 to {MaxUnitPriceCents}.");
            }
        }

        return problems;
    }
}
