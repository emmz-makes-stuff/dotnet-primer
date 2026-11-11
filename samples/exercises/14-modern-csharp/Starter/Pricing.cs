namespace ModernCSharp;

public class Pricing
{
    private readonly CustomerDirectory _directory;

    public Pricing(CustomerDirectory directory)
    {
        _directory = directory;
    }

    // TODO 4: the discount rules, in order. The first that matches wins:
    //   - an order whose only line is a gift card: no discount;
    //   - a Gold customer: 10%;
    //   - a subtotal of 100 or more: 5%;
    //   - anything else, including a customer the directory doesn't know: no discount.
    //   The chain below checks the total before the tier, so a Gold customer's large order gets 5%,
    //   and it has no gift-card rule. Rewrite it as one switch expression over (order, customer),
    //   with a list pattern for the single gift-card line and property patterns for the rest.
    //   The tests check each rule's result: they can't see whether a switch expression gave it.
    //   Read: https://dotnet-primer.emmz.dev/csharp/patterns/
    public decimal DiscountRate(Order order)
    {
        Customer customer = _directory.Find(order.CustomerId);
        bool isGold = customer.Tier == LoyaltyTier.Gold;

        if (order.Subtotal >= 100m)
        {
            return 0.05m;
        }
        else if (isGold)
        {
            return 0.10m;
        }
        else
        {
            return 0m;
        }
    }

    public Order Price(Order order)
    {
        return order.ApplyDiscount(DiscountRate(order));
    }
}
