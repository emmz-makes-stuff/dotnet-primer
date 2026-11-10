using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders;

/// <summary>The orders database.</summary>
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
}

/// <summary>The <see cref="Order"/> table: everything conventions don't decide.</summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> order)
    {
        // TODO 2: configure the order. By convention:
        //   - CustomerEmail is nvarchar(max). Give it a maximum length of 256, and an index.
        //   - Total gets no precision. Money is decimal(10,2).
        //   - Status is stored as a number. Store it by name, as a string of at most 20 characters.
        //   - The Lines relationship is found by convention, but say it here, so it's explicit:
        //     an order has many lines, each with an OrderId foreign key.
    }
}

/// <summary>The <see cref="OrderLine"/> table.</summary>
internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> line)
    {
        // TODO 2: configure the line. Sku has a maximum length of 32, and UnitPrice is money.
        // There's no DbSet<OrderLine>, so by convention the table is named "OrderLine", next to
        // "Orders". Name it "OrderLines".
    }
}
