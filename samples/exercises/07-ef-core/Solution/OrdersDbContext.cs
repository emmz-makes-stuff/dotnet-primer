using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders;

/// <summary>The orders database. Registered as scoped: one instance per request.</summary>
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
        // Without a maximum length, a string becomes nvarchar(max), which can't be indexed.
        order.Property(o => o.CustomerEmail).HasMaxLength(256);
        order.HasIndex(o => o.CustomerEmail);

        // Without a precision, EF warns and SQL Server uses decimal(18,2).
        order.Property(o => o.Total).HasPrecision(10, 2);

        // Store the status by name, so the column still reads correctly if the enum is reordered.
        order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

        order.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId);
    }
}

/// <summary>The <see cref="OrderLine"/> table.</summary>
internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> line)
    {
        // There's no DbSet<OrderLine>, so by convention the table would be named after the class,
        // "OrderLine", next to "Orders". Name it to match.
        line.ToTable("OrderLines");
        line.Property(l => l.Sku).HasMaxLength(32);
        line.Property(l => l.UnitPrice).HasPrecision(10, 2);
    }
}
