using System.Reflection;

namespace ModernCSharp.Tests;

/// <summary>
/// TODO 1: nullable reference types. Compiled into both Starter.Tests and Solution.Tests, so the
/// tests read the annotations of whichever library the project references.
/// </summary>
public sealed class NullSafetyTests
{
    private static readonly NullabilityInfoContext _nullability = new();

    [Fact]
    public void FindSaysItCanReturnNull()
    {
        var find = typeof(CustomerDirectory).GetMethod(nameof(CustomerDirectory.Find))!;
        var state = _nullability.Create(find.ReturnParameter).ReadState;

        Expect.That(state == NullabilityState.Nullable,
            $"CustomerDirectory.Find's return is {state}, expected Nullable: it returns null for a customer it doesn't know, "
            + "so its signature should say Customer?",
            1, Shop.NullSafetyPage);
    }

    [Fact]
    public void CustomerNameIsNeverNull()
    {
        var state = _nullability.Create(typeof(Customer).GetProperty(nameof(Customer.Name))!).ReadState;

        Expect.That(state == NullabilityState.NotNull,
            $"Customer.Name is {state}, expected NotNull: every customer has a name. Unknown means nullable is still "
            + "off in Customers.cs, so delete its #nullable disable",
            1, Shop.NullSafetyPage);
    }

    [Fact]
    public void CustomerEmailCanBeNull()
    {
        var state = _nullability.Create(typeof(Customer).GetProperty(nameof(Customer.Email))!).ReadState;

        Expect.That(state == NullabilityState.Nullable,
            $"Customer.Email is {state}, expected Nullable: a customer needn't give an email address, so it's string?",
            1, Shop.NullSafetyPage);
    }

    [Fact]
    public void UnknownCustomerIsPricedWithoutATierDiscount()
    {
        var order = Shop.OrderFor("nobody", Shop.Line("SKU-1", 2, 20m));

        var priced = Expect.Priced(order);

        Expect.That(priced.Discount == 0m && priced.Total == 40m,
            $"an order of 40.00 for a customer the directory doesn't know came to {priced.Total} with {priced.Discount:P0} off, "
            + "expected 40.00 with no discount",
            1, Shop.NullSafetyPage);
    }
}
