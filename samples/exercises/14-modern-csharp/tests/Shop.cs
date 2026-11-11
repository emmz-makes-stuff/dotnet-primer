namespace ModernCSharp.Tests;

/// <summary>
/// The customers and orders the tests price. Uses only object initializers and collection
/// expressions, so it compiles against the starter's classes and the solution's records alike.
/// </summary>
internal static class Shop
{
    public const string NullSafetyPage = "/csharp/null-safety/";
    public const string TypesPage = "/csharp/types/";
    public const string PatternsPage = "/csharp/patterns/";

    public static CustomerDirectory Customers()
    {
        var directory = new CustomerDirectory();
        directory.Add(new Customer { Id = "standard", Name = "Sam Standard", Email = "sam@example.com", Tier = LoyaltyTier.Standard });
        directory.Add(new Customer { Id = "gold", Name = "Grace Gold", Email = null, Tier = LoyaltyTier.Gold });
        return directory;
    }

    public static Pricing NewPricing() => new(Customers());

    public static OrderLine Line(string sku, int quantity, decimal unitPrice) =>
        new() { Sku = sku, Quantity = quantity, UnitPrice = unitPrice };

    public static Order OrderFor(string customerId, params OrderLine[] lines) =>
        new() { CustomerId = customerId, Lines = [.. lines] };
}
