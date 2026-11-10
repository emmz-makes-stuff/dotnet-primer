namespace TestingOrders.Checker;

/// <summary>
/// One seeded bug: which App file it replaces, where its copy is, and the hint for a test project
/// that misses it. The numbers and files match <c>bugs/SeededBugs.targets</c>.
/// </summary>
internal sealed record SeededBug(int Number, string Name, string Folder, string File, string Hint)
{
    public static readonly SeededBug[] All =
    [
        new(1, "the discount", "1-discount", "Pricing.cs",
            "TODO 1: a plain unit test of Pricing. Try subtotals at the edges of the discount rule, not just far from them."),
        new(2, "the warehouse", "2-warehouse", "OrderService.cs",
            "TODO 2: a test double for IWarehouse. Check what the warehouse was told, not only that it was called."),
        new(3, "the cancel window", "3-clock", "CancelPolicy.cs",
            "TODO 3: a FakeTimeProvider. Test a time inside the window as well as one after it."),
        new(4, "the service registrations", "4-registration", "OrderRegistration.cs",
            "TODO 4: a WebApplicationFactory test. Place an order, then read it back in a second request."),
    ];

    /// <summary>The marker that ends the one line a bug file changes.</summary>
    public string Marker => $"// SEEDED BUG {Number}:";
}
