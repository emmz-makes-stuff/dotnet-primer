---
title: EF Core cheatsheet
description: Registering a DbContext, fluent configuration, the dotnet ef commands, query and save patterns, concurrency, SQL logging and a local SQL Server, on one page.
prev: false
next: false
learn:
  - title: Entity Framework Core overview
    url: https://learn.microsoft.com/ef/core/
  - title: DbContext lifetime, configuration, and initialization
    url: https://learn.microsoft.com/ef/core/dbcontext-configuration/
  - title: EF Core tools reference (.NET CLI)
    url: https://learn.microsoft.com/ef/core/cli/dotnet
  - title: Applying migrations
    url: https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying
---

Back to [EF Core](/data/ef-core/).

## Packages

```sh
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design   # for the dotnet ef tool; not shipped
```

## Register a context

```csharp title="Program.cs"
// Scoped: one context per request. The usual choice in a web app.
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Orders")));

// For singletons and background work: create a short-lived context when you need one.
builder.Services.AddDbContextFactory<OrdersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Orders")));

// Reuses context instances across requests, for very high request rates.
builder.Services.AddDbContextPool<OrdersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Orders")));
```

Use one of the three for a given context, not all of them.

```csharp title="With a factory"
await using var db = await dbFactory.CreateDbContextAsync(ct);
```

```sh
# The connection string, kept out of appsettings.json (local development)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Orders" "Server=localhost,1433;Database=Orders;User Id=sa;Password=Your_strong_password1;TrustServerCertificate=True"
```

## Configure the model

```csharp title="OrderConfiguration.cs"
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> order)
    {
        order.ToTable("Orders");
        order.HasKey(o => o.Id);                                    // Id or <Type>Id is the key by convention
        order.Property(o => o.CustomerEmail).HasMaxLength(256);     // not nvarchar(max)
        order.HasIndex(o => o.CustomerEmail);
        order.Property(o => o.Total).HasPrecision(10, 2);           // decimal(10,2)
        order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        order.Property(o => o.Version).IsRowVersion();              // optimistic concurrency
        order.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId);
    }
}
```

```csharp title="OrdersDbContext.cs"
protected override void OnModelCreating(ModelBuilder modelBuilder) =>
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
```

## dotnet ef

Run these from the project folder. `dnx` runs the tool without installing it.

```sh
dnx dotnet-ef@10.0.14 migrations add AddOrderNotes          # create a migration from model changes
dnx dotnet-ef@10.0.14 migrations list                       # and whether each is applied
dnx dotnet-ef@10.0.14 migrations remove                     # the last one; checks the database
dnx dotnet-ef@10.0.14 migrations remove --force             # without checking the database
dnx dotnet-ef@10.0.14 database update                       # apply every pending migration
dnx dotnet-ef@10.0.14 database update InitialCreate         # go back to (or forward to) one
dnx dotnet-ef@10.0.14 migrations script --idempotent -o migrate.sql   # for production
dnx dotnet-ef@10.0.14 migrations bundle --force             # ./efbundle, a self-contained migrator
./efbundle --connection "<connection string>"
```

## Query

```csharp
// Read-only: no change tracking.
var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

// Only the columns you need: one query, never tracked.
var summaries = await db.Orders
    .Where(o => o.CustomerEmail == email)
    .Select(o => new OrderSummary(o.Id, o.Status, o.Total, o.Lines.Count))
    .ToListAsync(ct);

// Related data, loaded with the parent.
var withLines = await db.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == id, ct);
var deep = await db.Customers.Include(c => c.Orders).ThenInclude(o => o.Lines).ToListAsync(ct);

// Several collections: one query per collection instead of one wide join.
var split = await db.Orders.Include(o => o.Lines).Include(o => o.Payments).AsSplitQuery().ToListAsync(ct);

// The SQL a query will send, without running it.
var sql = db.Orders.Where(o => o.Status == OrderStatus.Placed).ToQueryString();

var exists = await db.Orders.AnyAsync(o => o.CustomerEmail == email, ct);
var count = await db.Orders.CountAsync(o => o.Status == OrderStatus.Placed, ct);
```

## Save

```csharp
db.Orders.Add(order);                  // INSERT, with its lines
order.Status = OrderStatus.Cancelled;  // a tracked entity: UPDATE of that column only
db.Orders.Remove(order);               // DELETE
await db.SaveChangesAsync(ct);         // all of the above, in one transaction

// Set-based: straight to SQL, no loading, no tracking, no SaveChanges.
await db.Orders.Where(o => o.PlacedAt < cutoff)
    .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Cancelled), ct);
await db.Orders.Where(o => o.Status == OrderStatus.Cancelled).ExecuteDeleteAsync(ct);
```

## Concurrency

```csharp title="Order.cs"
public byte[] Version { get; set; } = [];   // with IsRowVersion(): SQL Server's rowversion
```

```csharp
try
{
    await db.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    return Results.Conflict();   // someone else changed the row since you read it
}
```

## Log the SQL

```json title="appsettings.Development.json"
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

```csharp title="Program.cs (development only)"
options.UseSqlServer(connectionString).EnableSensitiveDataLogging();   // shows parameter values
```

## Local SQL Server

```sh
docker run -d --name sql --platform linux/amd64 \
  -e ACCEPT_EULA=Y -e 'MSSQL_SA_PASSWORD=Your_strong_password1' \
  -p 1433:1433 mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04
docker stop sql && docker rm sql
```

The image is x64 only: on Apple silicon it runs under emulation (see [Docker](/platform/docker/#macos)).
