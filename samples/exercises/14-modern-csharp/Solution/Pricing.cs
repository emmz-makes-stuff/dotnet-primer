namespace ModernCSharp;

public sealed class Pricing(CustomerDirectory directory)
{
    public decimal DiscountRate(Order order) =>
        (order, directory.Find(order.CustomerId)) switch
        {
            ({ Lines: [{ IsGiftCard: true }] }, _) => 0m,
            (_, { Tier: LoyaltyTier.Gold }) => 0.10m,
            ({ Subtotal: >= 100m }, _) => 0.05m,
            _ => 0m,
        };

    public Order Price(Order order) => order.ApplyDiscount(DiscountRate(order));
}
