#nullable disable
// TODO 1: turn nullable reference types on, a file at a time, starting with this one.
//   The project has <Nullable>enable</Nullable>, but the line above turns it off for this file.
//   Delete it, and the build fails with nullable warnings (warnings are errors in this repo):
//   - Id and Name are never null, so the compiler wants them set when a Customer is created. Mark
//     them required;
//   - Email can be missing, so make it string?;
//   - Find returns null for a customer it doesn't know: say so with Customer?, and give its local the
//     same type.
//   Once Find says Customer?, the compiler points at the dereference in Pricing.cs: make pricing an
//   order for an unknown customer work, with no tier discount and no exception.
//   Read: https://dotnet-primer.emmz.dev/csharp/null-safety/
namespace ModernCSharp;

public enum LoyaltyTier
{
    Standard,
    Gold,
}

public class Customer
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public LoyaltyTier Tier { get; set; }
}

public class CustomerDirectory
{
    private readonly Dictionary<string, Customer> _customers = new Dictionary<string, Customer>();

    public void Add(Customer customer)
    {
        _customers[customer.Id] = customer;
    }

    public Customer Find(string id)
    {
        Customer customer;
        if (_customers.TryGetValue(id, out customer))
        {
            return customer;
        }

        return null;
    }
}
