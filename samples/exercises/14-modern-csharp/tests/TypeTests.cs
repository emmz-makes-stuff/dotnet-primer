using System.Reflection;
using System.Runtime.CompilerServices;

namespace ModernCSharp.Tests;

/// <summary>
/// TODO 2 (records) and TODO 3 (immutability). They use Equals, ==, and reflection, never `with`
/// or positional syntax, so they compile against the starter's classes too.
/// </summary>
public sealed class TypeTests
{
    [Fact]
    public void LinesWithTheSameValuesAreEqual()
    {
        var first = Shop.Line("SKU-1", 2, 9.99m);
        var second = Shop.Line("SKU-1", 2, 9.99m);

        Expect.That(first.Equals(second),
            "two OrderLines with the same SKU, quantity and price aren't Equals: a class compares references. "
            + "Make OrderLine a record, so that it compares values",
            2, Shop.TypesPage);
        Expect.That(first == second,
            "two OrderLines with the same SKU, quantity and price aren't ==. A record's == compares values",
            2, Shop.TypesPage);
    }

    [Theory]
    [InlineData(nameof(OrderLine.Sku))]
    [InlineData(nameof(OrderLine.Quantity))]
    [InlineData(nameof(OrderLine.UnitPrice))]
    public void LineMembersAreRequired(string name)
    {
        var property = typeof(OrderLine).GetProperty(name)!;

        Expect.That(property.GetCustomAttribute<RequiredMemberAttribute>() is not null,
            $"OrderLine.{name} isn't required, so a line can be created without it. Mark it required",
            2, Shop.TypesPage);
    }

    [Theory]
    [InlineData(nameof(OrderLine.Sku))]
    [InlineData(nameof(OrderLine.Quantity))]
    [InlineData(nameof(OrderLine.UnitPrice))]
    public void LineMembersAreInitOnly(string name)
    {
        var setter = typeof(OrderLine).GetProperty(name)!.SetMethod;
        var initOnly = setter is not null
            && setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));

        Expect.That(initOnly,
            $"OrderLine.{name} has a set accessor, so a line can change after it's created. Use init",
            2, Shop.TypesPage);
    }

    [Fact]
    public void ApplyingADiscountLeavesTheOriginalAlone()
    {
        var original = Shop.OrderFor("standard", Shop.Line("SKU-1", 1, 50m));

        var discounted = original.ApplyDiscount(0.05m);

        Expect.That(!ReferenceEquals(original, discounted),
            "ApplyDiscount returned the order it was given, changed. Make Order a record, and return a copy with `with`",
            3, Shop.TypesPage);
        Expect.That(original.Discount == 0m,
            $"after ApplyDiscount, the original order's discount is {original.Discount:P0}, expected 0%: "
            + "applying a discount mustn't change the order it's applied to",
            3, Shop.TypesPage);
        Expect.That(discounted.Discount == 0.05m,
            $"the order ApplyDiscount returned has {discounted.Discount:P0} off, expected 5%",
            3, Shop.TypesPage);
    }
}
