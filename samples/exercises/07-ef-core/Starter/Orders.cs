namespace Orders;

/// <summary>Where an order is in its life.</summary>
public enum OrderStatus
{
    /// <summary>Saved and waiting to be fulfilled.</summary>
    Placed,

    /// <summary>Cancelled by the customer.</summary>
    Cancelled,
}

/// <summary>An order, as stored. Its lines are a one-to-many relationship.</summary>
public sealed class Order
{
    public int Id { get; set; }
    public required string CustomerEmail { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset PlacedAt { get; set; }
    public ICollection<OrderLine> Lines { get; } = [];
}

/// <summary>One line of an order, as stored.</summary>
public sealed class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public required string Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>The body of <c>POST /orders</c>.</summary>
public sealed record PlaceOrderRequest(string CustomerEmail, IReadOnlyList<OrderItem> Items);

/// <summary>One line of a <see cref="PlaceOrderRequest"/>.</summary>
public sealed record OrderItem(string Sku, int Quantity, decimal UnitPrice);

/// <summary>The body of a <c>POST /orders</c> response.</summary>
public sealed record PlaceOrderResponse(int OrderId);

/// <summary>One row of <c>GET /customers/{email}/orders</c>.</summary>
public sealed record OrderSummary(int Id, OrderStatus Status, decimal Total, int LineCount);
