namespace TestingOrders;

/// <summary>How much an order costs. Pure functions, with no dependencies.</summary>
public static class Pricing
{
    /// <summary>An order whose subtotal is at least this much, in cents ($100.00), gets 10% off.</summary>
    public const long DiscountThresholdCents = 10_000;

    /// <summary>The sum of each line's quantity times its unit price, in cents.</summary>
    public static long Subtotal(IEnumerable<OrderLine> lines) => lines.Sum(l => l.Quantity * l.UnitPriceCents);

    /// <summary>
    /// The total, in cents: 10% off a subtotal of <see cref="DiscountThresholdCents"/> or more, with the
    /// discount rounded down to a whole cent.
    /// </summary>
    public static long Total(long subtotalCents)
    {
        var discount = subtotalCents > DiscountThresholdCents ? subtotalCents / 10 : 0; // SEEDED BUG 1: > where the rule says "at least".
        return subtotalCents - discount;
    }
}
