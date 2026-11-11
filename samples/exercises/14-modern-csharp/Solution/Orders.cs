namespace ModernCSharp;

public sealed record OrderLine
{
    public required string Sku { get; init; }

    public required int Quantity { get; init; }

    public required decimal UnitPrice { get; init; }

    public decimal Total => Quantity * UnitPrice;

    public bool IsGiftCard => Sku.StartsWith("GIFT-", StringComparison.Ordinal);
}

public sealed record Order
{
    public required string CustomerId { get; init; }

    public required IReadOnlyList<OrderLine> Lines { get; init; }

    public decimal Discount { get; init; }

    public decimal Subtotal => Lines.Sum(line => line.Total);

    public decimal Total => Subtotal * (1 - Discount);

    public Order ApplyDiscount(decimal rate) => this with { Discount = rate };
}
