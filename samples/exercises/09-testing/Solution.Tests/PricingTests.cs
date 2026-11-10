using TestingOrders;

namespace TestingOrders.Tests;

// TODO 1 (solved): Pricing is pure, so plain unit tests are enough: no doubles, no app.
public class PricingTests
{
    [Fact]
    public void SubtotalIsQuantityTimesPriceForEveryLine()
    {
        OrderLine[] lines = [new("SKU-1", 2, 1_250), new("SKU-2", 1, 500)];

        Assert.Equal(3_000, Pricing.Subtotal(lines));
    }

    // The edges are where rules break: just below the threshold, exactly on it, and above it.
    [Theory]
    [InlineData(9_999, 9_999)]
    [InlineData(10_000, 9_000)]
    [InlineData(12_345, 11_111)]
    public void TotalTakesTenPercentOffAtOrAboveTheThreshold(long subtotalCents, long expectedTotalCents) =>
        Assert.Equal(expectedTotalCents, Pricing.Total(subtotalCents));
}
