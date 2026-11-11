#nullable disable
// Nullable is off here too, until TODOs 2 and 3 are done. Then delete the line above: with required
// members, the file builds clean.
namespace ModernCSharp;

// TODO 2: an order line is a value. Two lines with the same SKU, quantity and price should be equal,
//   and a line shouldn't change once it's created. Make OrderLine a record, and give each of Sku,
//   Quantity and UnitPrice `required` and `init` in place of `set`.
//   Read: https://dotnet-primer.emmz.dev/csharp/types/
public class OrderLine
{
    public string Sku { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Total
    {
        get { return Quantity * UnitPrice; }
    }

    public bool IsGiftCard
    {
        get { return Sku.StartsWith("GIFT-", StringComparison.Ordinal); }
    }
}

// TODO 3: ApplyDiscount changes this order and hands it back, so whoever priced it changed the
//   caller's order too. Make Order a record with `required` and `init` members, make Lines an
//   IReadOnlyList<OrderLine>, and have ApplyDiscount return a copy with `with`, leaving this
//   order alone.
//   Read: https://dotnet-primer.emmz.dev/csharp/types/
public class Order
{
    public string CustomerId { get; set; }

    public List<OrderLine> Lines { get; set; }

    public decimal Discount { get; set; }

    public decimal Subtotal
    {
        get
        {
            decimal subtotal = 0m;
            foreach (OrderLine line in Lines)
            {
                subtotal += line.Total;
            }

            return subtotal;
        }
    }

    public decimal Total
    {
        get { return Subtotal * (1 - Discount); }
    }

    public Order ApplyDiscount(decimal rate)
    {
        Discount = rate;
        return this;
    }
}
