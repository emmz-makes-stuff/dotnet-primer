using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Orders.Tests;

/// <summary>
/// Shared tests for exercise 07. Compiled into both Starter.Tests and Solution.Tests, so they run
/// against whichever app the project references. Against the starter they fail until its TODOs
/// are done.
/// </summary>
/// <remarks>
/// The tests run against a real SQL Server in a container (see <see cref="SqlServerFixture"/>), so
/// Docker must be running. Tests in one class run one at a time, and each uses its own customer,
/// so they don't see each other's orders.
/// </remarks>
public sealed class OrdersDatabaseTests(OrdersApp app) : IClassFixture<OrdersApp>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void ContextIsScopedAndUsesTheConfiguredConnectionString()
    {
        using var first = app.Services.CreateScope();
        using var second = app.Services.CreateScope();
        var a = first.ServiceProvider.GetService<OrdersDbContext>();
        Assert.True(
            a is not null,
            "OrdersDbContext isn't registered. In Program.cs, register it with builder.Services.AddDbContext<OrdersDbContext>(…).");

        Assert.True(
            ReferenceEquals(a, first.ServiceProvider.GetRequiredService<OrdersDbContext>()),
            "Within one request, OrdersDbContext should be the same instance everywhere. Register it with AddDbContext, which makes it scoped.");
        Assert.False(
            ReferenceEquals(a, second.ServiceProvider.GetRequiredService<OrdersDbContext>()),
            "Each request should get its own OrdersDbContext, but two requests shared one. A context isn't thread-safe: " +
            "register it with AddDbContext (scoped), never as a singleton.");

        // Compare where it points, not the text: SqlClient may normalise or drop parts of the string.
        var expected = new SqlConnectionStringBuilder(app.ConnectionString);
        var actual = new SqlConnectionStringBuilder(a.Database.GetConnectionString() ?? "");
        Assert.True(
            actual.DataSource == expected.DataSource && actual.InitialCatalog == expected.InitialCatalog,
            "OrdersDbContext isn't using the connection string named \"Orders\" from configuration. " +
            "Use options.UseSqlServer(builder.Configuration.GetConnectionString(\"Orders\")).");
    }

    [Fact]
    public async Task MigrationsMatchTheModel()
    {
        await app.MigrateAsync();

        await app.WithContextAsync(async db =>
        {
            var pending = await db.Database.GetPendingMigrationsAsync(Ct);
            Assert.True(!pending.Any(), $"Migrations weren't applied: {string.Join(", ", pending)}.");
            Assert.False(
                db.Database.HasPendingModelChanges(),
                "The model has changes that no migration covers. After changing the model or its configuration, " +
                "add a migration: `dnx dotnet-ef@10.0.14 migrations add <Name>` in the Starter folder. " +
                "(Before you've shipped it, you can also remove the last one with `migrations remove --force` and add it again.)");
            return true;
        });
    }

    [Fact]
    public async Task ModelIsConfigured()
    {
        await app.WithContextAsync(db =>
        {
            var order = db.Model.FindEntityType(typeof(Order));
            var line = db.Model.FindEntityType(typeof(OrderLine));
            Assert.True(order is not null && line is not null, "Order and OrderLine should both be in the model.");

            var email = order.FindProperty(nameof(Order.CustomerEmail))!;
            Assert.True(
                email.GetMaxLength() is not null,
                "Order.CustomerEmail has no maximum length, so it's stored as nvarchar(max), which can't be indexed. " +
                "In OrderConfiguration, give it HasMaxLength(256).");

            foreach (var (entity, name) in new[] { (order, nameof(Order.Total)), (line, nameof(OrderLine.UnitPrice)) })
            {
                var money = entity.FindProperty(name)!;
                Assert.True(
                    money.GetColumnType() == "decimal(10,2)",
                    $"{entity.ClrType.Name}.{name} is stored as {money.GetColumnType()}, but money should be decimal(10,2). " +
                    "Use HasPrecision(10, 2).");
            }

            var status = order.FindProperty(nameof(Order.Status))!;
            Assert.True(
                status.GetProviderClrType() == typeof(string),
                "Order.Status is stored as a number, so reordering the enum would change what every row means. " +
                "Store it by name with HasConversion<string>().");

            var lines = order.FindNavigation(nameof(Order.Lines));
            Assert.True(
                lines is { IsCollection: true } && lines.ForeignKey.Properties.Single().Name == nameof(OrderLine.OrderId),
                "Order.Lines should be a one-to-many relationship, keyed by OrderLine.OrderId. " +
                "In OrderConfiguration: HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId).");
            return Task.FromResult(true);
        });
    }

    [Fact]
    public async Task OrderAndLinesAreSavedTogether()
    {
        await app.MigrateAsync();
        var email = NewCustomer();
        using var client = app.CreateClient();

        app.Saves.Reset();
        using var response = await client.PostAsJsonAsync(
            "/orders",
            new PlaceOrderRequest(email, [new("SKU-1", 2, 4.99m), new("SKU-2", 1, 10.50m), new("SKU-3", 3, 0.33m)]),
            Ct);
        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"POST /orders should return 201 Created, but returned {(int)response.StatusCode}.");
        Assert.True(
            app.Saves.Count == 1,
            $"Placing one order called SaveChanges {app.Saves.Count} times. Add the order with all its lines, then call " +
            "SaveChangesAsync once: one SaveChanges is one transaction, so the order and its lines are saved together or not at all.");

        var id = (await response.Content.ReadFromJsonAsync<PlaceOrderResponse>(Ct))!.OrderId;
        var stored = await app.WithContextAsync(db =>
            db.Orders.AsNoTracking().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, Ct));

        Assert.True(stored is not null, $"Order {id} isn't in the database.");
        Assert.True(stored.Lines.Count == 3, $"Order {id} should have 3 lines in the database, but has {stored.Lines.Count}.");
        Assert.True(stored.Total == 21.47m, $"The order's total should be stored as exactly 21.47, but was {stored.Total}.");
        Assert.True(
            stored.Lines.Select(l => l.UnitPrice).Order().SequenceEqual([0.33m, 4.99m, 10.50m]),
            "The lines' unit prices weren't stored exactly as sent.");
    }

    [Fact]
    public async Task ListingOrdersAvoidsNPlusOne()
    {
        await app.MigrateAsync();
        var email = NewCustomer();
        await app.WithContextAsync(async db =>
        {
            for (var i = 1; i <= 5; i++)
            {
                var order = new Order { CustomerEmail = email, Total = i, PlacedAt = DateTimeOffset.UtcNow.AddMinutes(i) };
                for (var l = 0; l < i; l++)
                {
                    order.Lines.Add(new OrderLine { Sku = $"SKU-{l}", Quantity = 1, UnitPrice = 1m });
                }

                db.Orders.Add(order);
            }

            return await db.SaveChangesAsync(Ct);
        });

        using var client = app.CreateClient();
        app.Selects.Reset();
        var orders = await client.GetFromJsonAsync<List<Summary>>($"/customers/{email}/orders", Ct) ?? [];
        var selects = app.Selects.Count;

        Assert.True(orders.Count == 5, $"GET /customers/{{email}}/orders should list the customer's 5 orders, but listed {orders.Count}.");
        Assert.True(
            orders.Select(o => o.LineCount).SequenceEqual([5, 4, 3, 2, 1]),
            $"Each order should report its line count, newest first: expected 5, 4, 3, 2, 1, but got {string.Join(", ", orders.Select(o => o.LineCount))}.");
        Assert.True(
            selects == 1,
            $"Listing 5 orders sent {selects} SELECT commands to the database: one for the list, then one per order. That's N+1. " +
            "Project each order with Select(o => new OrderSummary(…, o.Lines.Count)) so EF Core counts the lines in the same query.");
    }

    [Fact]
    public async Task CancellingUsesChangeTracking()
    {
        await app.MigrateAsync();
        using var client = app.CreateClient();
        using var placed = await client.PostAsJsonAsync("/orders", new PlaceOrderRequest(NewCustomer(), [new("SKU-1", 1, 5m)]), Ct);
        var id = (await placed.Content.ReadFromJsonAsync<PlaceOrderResponse>(Ct))!.OrderId;

        app.Saves.Reset();
        using var cancelled = await client.PostAsync($"/orders/{id}/cancel", null, Ct);
        Assert.True(
            cancelled.StatusCode == HttpStatusCode.NoContent,
            $"POST /orders/{{id}}/cancel should return 204 No Content, but returned {(int)cancelled.StatusCode}." +
            (cancelled.StatusCode == HttpStatusCode.NotImplemented ? " (501 is the Starter's placeholder: TODO 6 isn't done yet.)" : ""));
        Assert.True(
            app.Saves.Count == 1,
            "Cancel should go through change tracking: load the order with FindAsync, set its Status, and call SaveChangesAsync once.");

        var status = await app.WithContextAsync(db =>
            db.Orders.Where(o => o.Id == id).Select(o => o.Status).SingleAsync(Ct));
        Assert.True(status == OrderStatus.Cancelled, $"After cancelling, the order's stored status should be Cancelled, but was {status}.");

        using var unknown = await client.PostAsync("/orders/2147483000/cancel", null, Ct);
        Assert.True(
            unknown.StatusCode == HttpStatusCode.NotFound,
            $"Cancelling an order that doesn't exist should return 404 Not Found, but returned {(int)unknown.StatusCode}.");
    }

    private static string NewCustomer() => $"customer-{Guid.NewGuid():N}@example.test";

    /// <summary>A row of the list, read as JSON: the status arrives as its name.</summary>
    private sealed record Summary(int Id, string Status, decimal Total, int LineCount);
}
