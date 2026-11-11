namespace ModernCSharp.Tests;

internal static class Expect
{
    /// <summary>Fails with what's wrong, the TODO that fixes it, and the page that explains it.</summary>
    public static void That(bool condition, string whatsWrong, int todo, string page)
    {
        if (!condition)
        {
            Assert.Fail($"{whatsWrong} (TODO {todo}: see {page})");
        }
    }

    /// <summary>
    /// Prices an order. A NullReferenceException means the pricing dereferenced a customer that
    /// wasn't found, which is TODO 1's job to stop, so it's reported as that, not as a crash.
    /// </summary>
    public static Order Priced(Order order)
    {
        try
        {
            return Shop.NewPricing().Price(order);
        }
        catch (NullReferenceException)
        {
            Assert.Fail(
                $"pricing the order for customer '{order.CustomerId}' threw NullReferenceException. The directory "
                + "returns null for a customer it doesn't know, and the pricing dereferences it anyway. Annotate "
                + $"CustomerDirectory.Find as returning Customer?, and the compiler shows you where (TODO 1: see {Shop.NullSafetyPage})");
            throw;
        }
    }
}
