namespace Orders;

/// <summary>The body of <c>POST /orders</c>.</summary>
public sealed record PlaceOrderRequest(
    string CustomerEmail,
    string PaymentMethod,
    IReadOnlyList<OrderItem> Items,
    decimal Total);

/// <summary>One line of an order.</summary>
public sealed record OrderItem(string Sku, int Quantity);

/// <summary>The body of every <c>POST /orders</c> response.</summary>
public sealed record PlaceOrderResponse(string OrderId, string Status, string? Reason = null);
