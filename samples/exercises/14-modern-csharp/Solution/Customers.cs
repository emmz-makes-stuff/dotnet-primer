namespace ModernCSharp;

public enum LoyaltyTier
{
    Standard,
    Gold,
}

public sealed class Customer
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Email { get; init; }

    public LoyaltyTier Tier { get; init; }
}

public sealed class CustomerDirectory
{
    private readonly Dictionary<string, Customer> _customers = [];

    public void Add(Customer customer) => _customers[customer.Id] = customer;

    public Customer? Find(string id) => _customers.GetValueOrDefault(id);
}
