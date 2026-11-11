namespace ModernCSharp.Tests;

/// <summary>
/// TODO 4: the discount rules, one test per rule. They check each rule's result. They can't see
/// whether a switch expression produced it.
/// </summary>
public sealed class PatternTests
{
    [Fact]
    public void GiftCardOnlyOrderGetsNoDiscount()
    {
        ExpectDiscount(Shop.OrderFor("gold", Shop.Line("GIFT-50", 3, 50m)), 0m,
            "a Gold customer's order whose only line is a gift card. Gift cards are never discounted, so this rule "
            + "comes first. A list pattern matches an order with exactly one line");
    }

    [Fact]
    public void GiftCardAmongOtherLinesIsDiscounted()
    {
        ExpectDiscount(Shop.OrderFor("standard", Shop.Line("GIFT-50", 1, 50m), Shop.Line("SKU-1", 1, 70m)), 0.05m,
            "a Standard customer's order of 120.00 with a gift card and another line. Only an order whose single "
            + "line is a gift card goes without");
    }

    [Fact]
    public void GoldCustomerGetsTenPercentOnALargeOrder()
    {
        ExpectDiscount(Shop.OrderFor("gold", Shop.Line("SKU-1", 3, 50m)), 0.10m,
            "a Gold customer's order of 150.00. The Gold rule comes before the total rule");
    }

    [Fact]
    public void GoldCustomerGetsTenPercentOnASmallOrder()
    {
        ExpectDiscount(Shop.OrderFor("gold", Shop.Line("SKU-1", 1, 40m)), 0.10m,
            "a Gold customer's order of 40.00");
    }

    [Fact]
    public void OrderOfOneHundredGetsFivePercent()
    {
        ExpectDiscount(Shop.OrderFor("standard", Shop.Line("SKU-1", 4, 25m)), 0.05m,
            "a Standard customer's order of exactly 100.00. The total rule is 100 or more");
    }

    [Fact]
    public void SmallOrderGetsNoDiscount()
    {
        ExpectDiscount(Shop.OrderFor("standard", Shop.Line("SKU-1", 1, 99.99m)), 0m,
            "a Standard customer's order of 99.99");
    }

    private static void ExpectDiscount(Order order, decimal expected, string theCase)
    {
        var priced = Expect.Priced(order);

        Expect.That(priced.Discount == expected,
            $"{theCase}: got {priced.Discount:P0} off, expected {expected:P0}",
            4, Shop.PatternsPage);
    }
}
