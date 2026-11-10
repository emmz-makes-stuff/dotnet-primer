namespace Orders;

/// <summary>Where an order is in its life.</summary>
public enum OrderStatus
{
    /// <summary>Saved and waiting to be fulfilled.</summary>
    Placed,

    /// <summary>Cancelled by the customer.</summary>
    Cancelled,
}

/// <summary>An order, as stored. Its lines are a one-to-many relationship.</summary>
public sealed class Order
{
    public int Id { get; set; }
    public required string CustomerEmail { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset PlacedAt { get; set; }
    public ICollection<OrderLine> Lines { get; } = [];
}

/// <summary>One line of an order, as stored.</summary>
public sealed class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public required string Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>The body of <c>POST /orders</c>.</summary>
public sealed record PlaceOrderRequest(string CustomerEmail, IReadOnlyList<OrderItem> Items)
{
    /// <summary>The largest amount a <c>decimal(10,2)</c> money column holds.</summary>
    public const decimal MaxMoney = 99_999_999.99m;

    /// <summary>
    /// What's wrong with the request, by field, or nothing if it can be stored as it is. The limits
    /// match the model's configuration, so a bad value gets a 400 here, not a 500 from the database.
    /// </summary>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        // JSON can send null for any of these, whatever the C# types say.
        if (string.IsNullOrWhiteSpace(CustomerEmail))
        {
            errors["customerEmail"] = ["A customer email is required."];
        }
        else if (CustomerEmail.Length > 256)
        {
            errors["customerEmail"] = ["A customer email can be at most 256 characters."];
        }

        if (Items is not { Count: > 0 })
        {
            errors["items"] = ["An order needs at least one item."];
            return errors;
        }

        for (var i = 0; i < Items.Count; i++)
        {
            var problems = Items[i]?.Problems() ?? ["An item can't be empty."];
            if (problems.Count > 0)
            {
                errors[$"items[{i}]"] = [.. problems];
            }
        }

        // Only add up items that are valid, so a huge price can't overflow the sum.
        if (errors.Count == 0 && Items.Sum(i => i.Quantity * i.UnitPrice) > MaxMoney)
        {
            errors["items"] = [$"An order's total can be at most {MaxMoney:N2}."];
        }

        return errors;
    }
}

/// <summary>One line of a <see cref="PlaceOrderRequest"/>.</summary>
public sealed record OrderItem(string Sku, int Quantity, decimal UnitPrice)
{
    /// <summary>What's wrong with this item: empty if nothing is.</summary>
    public List<string> Problems()
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(Sku))
        {
            problems.Add("A SKU is required.");
        }
        else if (Sku.Length > 32)
        {
            problems.Add("A SKU can be at most 32 characters.");
        }

        if (Quantity < 1)
        {
            problems.Add("The quantity must be at least 1.");
        }

        if (UnitPrice < 0)
        {
            problems.Add("The unit price can't be negative.");
        }
        else if (UnitPrice > PlaceOrderRequest.MaxMoney)
        {
            problems.Add($"The unit price can be at most {PlaceOrderRequest.MaxMoney:N2}.");
        }
        else if (UnitPrice != decimal.Round(UnitPrice, 2))
        {
            // Rejected, not rounded, so the stored total is always the sum of the stored lines.
            problems.Add("The unit price can have at most 2 decimal places.");
        }

        return problems;
    }
}

/// <summary>The body of a <c>POST /orders</c> response.</summary>
public sealed record PlaceOrderResponse(int OrderId);

/// <summary>One row of <c>GET /customers/{email}/orders</c>.</summary>
public sealed record OrderSummary(int Id, OrderStatus Status, decimal Total, int LineCount);
