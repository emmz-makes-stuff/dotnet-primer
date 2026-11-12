---
title: "EF Core"
description: "Entity Framework Core with SQL Server: DbContext and its lifetime, modelling with the fluent API, migrations in development and production, LINQ and the SQL it sends, change tracking, and coming from Entity Framework 6."
learn:
  - title: "Entity Framework Core overview"
    url: https://learn.microsoft.com/ef/core/
  - title: "DbContext lifetime, configuration, and initialization"
    url: https://learn.microsoft.com/ef/core/dbcontext-configuration/
  - title: "DbContext pooling"
    url: https://learn.microsoft.com/ef/core/performance/advanced-performance-topics#dbcontext-pooling
  - title: "Migrations overview"
    url: https://learn.microsoft.com/ef/core/managing-schemas/migrations/
  - title: "Applying migrations"
    url: https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying
  - title: "Efficient querying"
    url: https://learn.microsoft.com/ef/core/performance/efficient-querying
  - title: "Handling concurrency conflicts"
    url: https://learn.microsoft.com/ef/core/saving/concurrency
  - title: "Porting from EF6 to EF Core"
    url: https://learn.microsoft.com/ef/efcore-and-ef6/porting/
---

By the end of this page you'll be able to give an ASP.NET Core app a SQL Server database through EF Core: register a `DbContext` with the right lifetime, describe the schema in C#, change it with migrations and ship those safely, write queries knowing the SQL they send, and save changes deliberately, one unit of work at a time.

:::tip[Cheatsheet]
Keep the [EF Core cheatsheet](/cheatsheets/ef-core/) open while you work.
:::

The examples use one small model, the orders from the [Observability](/observability/logging/) pages: an `Order` with a customer email, a status, a total, and its `OrderLine`s.

## A local SQL Server

You need [Docker](/platform/docker/). Start SQL Server in a container:

```sh
docker run -d --name sql --platform linux/amd64 \
  -e ACCEPT_EULA=Y -e 'MSSQL_SA_PASSWORD=Your_strong_password1' \
  -p 1433:1433 mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04
```

The first run downloads about 650 MB, which unpacks to 1.8 GB. SQL Server is ready when `docker logs sql` says "SQL Server is now ready for client connections", 10 to 15 seconds later. The image is for x64 only: on an Apple silicon Mac it runs under emulation, which your Docker tool may need switching on (see [Docker](/platform/docker/#macos)). The password is for this local container only.

Keep the connection string out of `appsettings.json`, in user secrets, as on the [Configuration](/aspnet-core/configuration/) page:

```sh
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Orders" "Server=localhost,1433;Database=Orders;User Id=sa;Password=Your_strong_password1;TrustServerCertificate=True"
```

`TrustServerCertificate=True` accepts the container's self-signed certificate. Don't use it against a real server.

## DbContext and its lifetime

Add the provider package, and the design package the `dotnet ef` tool needs:

```sh
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
```

A `DbContext` is one conversation with the database. It holds a connection, the entities it has loaded, and the changes you've made to them since. Yours derives from it, with a `DbSet<T>` for each table you query:

```csharp title="OrdersDbContext.cs"
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
}
```

Register it with `AddDbContextPool`. Each request still gets its own context, shared by everything in that request. But when the request ends, the context isn't thrown away: EF Core resets it and keeps it in a **pool** for a later request. That saves setting up a new context for every request, which adds up when there are many. This is from the exercise:

```csharp title="Program.cs"
// Pooled: each request still gets its own context, and its change tracker, but a finished request's
// context is reset and reused instead of being built again.
builder.Services.AddDbContextPool<OrdersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Orders")));
```

Then ask for it like any other service, here in a minimal API endpoint:

```csharp title="Program.cs"
app.MapGet("/orders/{id:int}", async (int id, OrdersDbContext db, CancellationToken ct) =>
    await db.Orders.FindAsync([id], ct) is { } order ? Results.Ok(order) : Results.NotFound());
```

**A pooled context holds no per-request state.** The pool creates each context once and reuses it, so its constructor runs once, with services from the app's root provider rather than the request's. Give it only its `DbContextOptions`, as above. If it also asks for a scoped service, it fails in Development with "Cannot resolve scoped service '…' from root provider", and elsewhere it gets an instance that isn't the request's. Pass per-request values to the code that uses the context instead. If a context really must hold them, register it with `AddDbContext`, which builds a new one for every request.

**A context is not thread-safe**, and it remembers everything it loads. So never share one: not as a singleton, not across requests, and not between two queries running at once with `Task.WhenAll`. Code that lives longer than a request, such as a singleton or a `BackgroundService`, uses a pooled factory and rents a short-lived context each time:

```csharp title="Program.cs"
builder.Services.AddPooledDbContextFactory<OrdersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Orders")));
```

```csharp title="OrderSweeper.cs"
// Disposing the context returns it to the pool.
await using var db = await dbFactory.CreateDbContextAsync(ct);
```

`AddPooledDbContextFactory` also registers the context itself for requests, from the same pool, so endpoints can still take `OrdersDbContext`. Register one or the other: `AddDbContextPool` when only requests use the context, `AddPooledDbContextFactory` when longer-lived code does too.

## Modelling

Entities are plain classes. EF Core reads them by **convention**: a property called `Id` (or `OrderId` on `Order`) is the key, an `int` key is an identity column, a collection of another entity is a one-to-many relationship, and `OrderId` on `OrderLine` is its foreign key. With [nullable reference types](/csharp/null-safety/) on, as they are in every new project, a `string` is a required, `NOT NULL` column, and a `string?` is optional.

```csharp title="Orders.cs"
public sealed class Order
{
    public int Id { get; set; }
    public required string CustomerEmail { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset PlacedAt { get; set; }
    public ICollection<OrderLine> Lines { get; } = [];
}

public sealed class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public required string Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

Conventions don't know your data, though. Left alone, a `string` becomes `nvarchar(max)`, which SQL Server can't index, a `decimal` gets no precision (EF Core warns, and maps it to `decimal(18,2)`), and an enum is stored as a number, which changes meaning if someone reorders it. Say what you mean with the **fluent API**, one configuration class per entity. This is the exercise's:

```csharp title="OrdersDbContext.cs"
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> order)
    {
        // Without a maximum length, a string becomes nvarchar(max), which can't be indexed.
        order.Property(o => o.CustomerEmail).HasMaxLength(256);
        order.HasIndex(o => o.CustomerEmail);

        // Without a precision, EF warns and maps it to decimal(18,2).
        order.Property(o => o.Total).HasPrecision(10, 2);

        // Store the status by name, so the column still reads correctly if the enum is reordered.
        order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

        order.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId);
    }
}
```

The relationship would have been found by convention anyway. Configuring it makes it explicit, in the one place a reader looks. `ApplyConfigurationsFromAssembly` in `OnModelCreating` picks up every configuration class.

You'll also see **data annotations**, attributes on the entity itself, such as `[MaxLength(256)]` and `[Precision(10, 2)]`. They work, but they cover less than the fluent API, and they mix database detail into your classes. Pick one style per project. This primer uses the fluent API.

## Migrations

EF Core doesn't change your schema by itself. A **migration** is C# code that does, generated by comparing your model with a snapshot of the last one. Run the `dotnet ef` tool with `dnx`, which fetches it without installing anything, from the project folder:

```sh
dnx dotnet-ef@10.0.14 migrations add InitialCreate
```

This writes three files to `Migrations/`: the migration itself, with an `Up` and a `Down` method, its designer file, and the updated model snapshot. Commit all three. **Read the migration before you apply it.** This one, for example, showed that the lines table would be called `OrderLine` next to `Orders`, because a table is named after its `DbSet` and there's no `DbSet<OrderLine>`. One line in the configuration fixed it:

```csharp title="OrdersDbContext.cs"
line.ToTable("OrderLines");
```

Then remove the migration and add it again (or, once it's been applied anywhere, add a second migration). Apply migrations to your local database with:

```sh
dnx dotnet-ef@10.0.14 database update
```

EF Core records each applied migration in a `__EFMigrationsHistory` table, so `database update` only runs what's new. Change the model, add a migration, review it, update: that's the loop.

### In production

Don't have the app migrate its own database at startup with `Database.Migrate()`. EF Core takes a lock while it migrates, so two instances starting together won't both run it, but the app then needs permission to change the schema, every instance waits on the lock to start, a failed migration stops all of them, and nobody reviewed the SQL that ran against production. Apply migrations as a separate deployment step instead, with one of these:

```sh
# A SQL script that checks the history table first, so it's safe to run against any version
dnx dotnet-ef@10.0.14 migrations script --idempotent -o migrate.sql

# Or a single executable that applies pending migrations
dnx dotnet-ef@10.0.14 migrations bundle --force
./efbundle --connection "<connection string>"
```

The script can be reviewed, and run by whoever owns the database. The bundle needs no .NET SDK where it runs, which suits a CI/CD pipeline. Either runs before the new version of the app starts.

## Querying

A LINQ query against a `DbSet` isn't run in .NET. EF Core translates it to SQL and sends that when you ask for results with `ToListAsync`, `FirstOrDefaultAsync`, `CountAsync` or similar. Use the async methods: a query waits on the network, and the async version frees the thread while it does.

```csharp
var placed = await db.Orders
    .Where(o => o.CustomerEmail == email && o.Status == OrderStatus.Placed)
    .ToListAsync(ct);
```

```sql
SELECT [o].[Id], [o].[CustomerEmail], [o].[PlacedAt], [o].[Status], [o].[Total]
FROM [Orders] AS [o]
WHERE [o].[CustomerEmail] = @email AND [o].[Status] = N'Placed'
```

The variable became a parameter, `@email`, so the query plan is reused and there's no SQL injection. **Look at the SQL your queries send**, at least while you're learning. Turn on command logging for development:

```json title="appsettings.Development.json"
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

Every command then appears in the console as `Executed DbCommand (3ms) [Parameters=[@email='?' (Size = 256)], …]`, followed by its SQL. `ToQueryString()` shows a query's SQL without running it, which is handy in a test or the debugger.

### Related data: Include or Select

Lazy loading is off: an order's `Lines` stay empty unless you ask for them. `Include` loads whole related entities with their parent, in a join:

```csharp
var orders = await db.Orders.AsNoTracking()
    .Include(o => o.Lines)
    .Where(o => o.CustomerEmail == email)
    .ToListAsync(ct);
```

```sql
SELECT [o].[Id], [o].[CustomerEmail], [o].[PlacedAt], [o].[Status], [o].[Total], [o0].[Id], [o0].[OrderId], [o0].[Quantity], [o0].[Sku], [o0].[UnitPrice]
FROM [Orders] AS [o]
LEFT JOIN [OrderLines] AS [o0] ON [o].[Id] = [o0].[OrderId]
WHERE [o].[CustomerEmail] = @email
ORDER BY [o].[Id]
```

When you only need some values, **project** with `Select`. EF Core then fetches only those columns, can compute in SQL, and tracks nothing, because there are no entities. This is the exercise's list:

```csharp title="Program.cs"
await db.Orders
    .Where(o => o.CustomerEmail == email)
    .OrderByDescending(o => o.PlacedAt)
    .Select(o => new OrderSummary(o.Id, o.Status, o.Total, o.Lines.Count))
    .ToListAsync(ct);
```

```sql
SELECT [o].[Id], [o].[Status], [o].[Total], (
    SELECT COUNT(*)
    FROM [OrderLines] AS [o0]
    WHERE [o].[Id] = [o0].[OrderId])
FROM [Orders] AS [o]
WHERE [o].[CustomerEmail] = @email
ORDER BY [o].[PlacedAt] DESC
```

`AsNoTracking()`, as on the `Include` query above, tells EF Core not to track the entities it returns. Use it for every read whose results you won't change and save: it's faster, and uses less memory.

### N+1 queries

The most common EF Core performance bug looks innocent:

```csharp
var orders = await db.Orders.Where(o => o.CustomerEmail == email).ToListAsync(ct);
foreach (var order in orders)
{
    var lineCount = await db.Set<OrderLine>().CountAsync(l => l.OrderId == order.Id, ct);
    // …
}
```

It sends one query for the orders, then one more for each order: N+1. With three orders, the log shows four commands:

```sql
SELECT [o].[Id], [o].[CustomerEmail], [o].[PlacedAt], [o].[Status], [o].[Total]
FROM [Orders] AS [o]
WHERE [o].[CustomerEmail] = @email

SELECT COUNT(*)
FROM [OrderLines] AS [o]
WHERE [o].[OrderId] = @order_Id

SELECT COUNT(*)
FROM [OrderLines] AS [o]
WHERE [o].[OrderId] = @order_Id

SELECT COUNT(*)
FROM [OrderLines] AS [o]
WHERE [o].[OrderId] = @order_Id
```

It works with ten rows in development, and falls over with ten thousand in production. The projection above does the same job in one command. A query inside a loop is the thing to look for.

### When a query can't be translated

Not every C# method has a SQL equivalent. EF Core translates a lot, including string methods, date parts and collections, but if a query calls your own method, it stops and says so:

```csharp
var vips = await db.Orders.Where(o => Customers.IsVip(o.CustomerEmail)).ToListAsync(ct);
```

```txt
InvalidOperationException: The LINQ expression 'DbSet<Order>()
    .Where(o => Customers.IsVip(o.CustomerEmail))' could not be translated. Additional information:
Translation of method 'Customers.IsVip' failed. … Either rewrite the query in a form that can be
translated, or switch to client evaluation explicitly by inserting a call to 'AsEnumerable',
'AsAsyncEnumerable', 'ToList', or 'ToListAsync'.
```

Rewrite the condition in terms SQL knows (`o.CustomerEmail.EndsWith("@vip.test")` translates to `LIKE`). Switching to client evaluation fetches every row first, so only do it after filtering in SQL.

## Change tracking and saving

A context tracks every entity it loads or you add. Each has a **state**: `Added`, `Unchanged`, `Modified` or `Deleted`. `SaveChangesAsync` turns every pending change into SQL and runs it **in one transaction**: all of it is saved, or none of it. That makes one `SaveChanges` call one unit of work. This is the exercise's order placement, abridged:

```csharp title="Program.cs"
var order = new Order { CustomerEmail = request.CustomerEmail, /* … */ };
foreach (var item in request.Items)
{
    order.Lines.Add(new OrderLine { Sku = item.Sku, Quantity = item.Quantity, UnitPrice = item.UnitPrice });
}

// One SaveChanges is one unit of work: the order and its lines are saved together, or not at all.
db.Orders.Add(order);
await db.SaveChangesAsync(ct);
```

EF Core inserts the order, reads back its new `Id`, sets `OrderId` on each line for you, and inserts the lines in one batch. Calling `SaveChangesAsync` after each line instead would work, but leave half an order behind if a later line failed.

To change a row, load it tracked, change it, and save. EF Core compares the entity with what it loaded, and updates only the columns that changed. This is the exercise's cancel:

```csharp title="Program.cs"
// FindAsync returns a tracked entity, so changing it is all SaveChanges needs.
var order = await db.Orders.FindAsync([id], ct);
if (order is null)
{
    return Results.NotFound();
}

order.Status = OrderStatus.Cancelled;
await db.SaveChangesAsync(ct);
```

```sql
SET IMPLICIT_TRANSACTIONS OFF;
SET NOCOUNT ON;
UPDATE [Orders] SET [Status] = @p0
OUTPUT 1
WHERE [Id] = @p1;
```

`OUTPUT 1` lets EF Core check that the update really changed a row. `Remove(entity)` marks an entity `Deleted` in the same way.

### Set-based changes

To change many rows at once, don't load them. `ExecuteUpdateAsync` and `ExecuteDeleteAsync` send one statement straight away, without tracking and without `SaveChanges`:

```csharp
await db.Orders
    .Where(o => o.Status == OrderStatus.Placed && o.PlacedAt < cutoff)
    .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Cancelled), ct);
```

```sql
UPDATE [o]
SET [o].[Status] = @p
FROM [Orders] AS [o]
WHERE [o].[Status] = N'Placed' AND [o].[PlacedAt] < @cutoff
```

Because they bypass the change tracker, entities already loaded in the context don't see the change.

### Concurrency

Two people load the same order, and both save. Without a check, the second save silently overwrites the first. A **concurrency token** stops that. On SQL Server, a `rowversion` column changes on every update:

```csharp title="Orders.cs"
public byte[] Version { get; set; } = [];
```

```csharp title="OrdersDbContext.cs"
order.Property(o => o.Version).IsRowVersion();
```

Every `UPDATE` and `DELETE` then checks it still has the version it loaded:

```sql
SET IMPLICIT_TRANSACTIONS OFF;
SET NOCOUNT ON;
UPDATE [Orders] SET [Total] = @p0
OUTPUT INSERTED.[Version]
WHERE [Id] = @p1 AND [Version] = @p2;
```

If someone else saved first, no row matches, and `SaveChangesAsync` throws:

```txt
DbUpdateConcurrencyException: The database operation was expected to affect 1 row(s), but actually
affected 0 row(s); data may have been modified or deleted since entities were loaded.
```

Catch it where you can decide what to do. For an API, that usually means telling the client:

```csharp title="Program.cs"
try
{
    await db.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    return Results.Conflict();
}
```

## Coming from Entity Framework 6

| In EF6                                                   | In EF Core                                                                                          |
| -------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| An EDMX model and the designer, or code first            | Code first only. There's no designer: the model is C#, kept in step with the database by migrations |
| Lazy loading on by default, through `virtual` properties | Off by default. Load related data with `Include`, or project with `Select`                          |
| `ObjectContext`, or a `DbContext` you `new` up           | A `DbContext` from dependency injection, one per request                                            |
| Migrations, with the model stored in a resource          | Migrations with a C# model snapshot, run with the `dotnet ef` tool                                  |
| `NotSupportedException` for LINQ it can't translate      | A translation error that names the method, with much more of LINQ translated                        |
| `System.Data.Entity`, from the EntityFramework package   | `Microsoft.EntityFrameworkCore`, with one package per database provider                             |

Many APIs keep their names, such as `DbSet`, `Include`, `SaveChanges` and `AsNoTracking`, but EF Core is a rewrite, not EF7. Port by rebuilding the model, not by renaming namespaces. Learn's [porting guide](https://learn.microsoft.com/ef/efcore-and-ef6/porting/) lists the behaviour that changed.

## Pitfalls

- **A context shared across requests or threads.** Registered as a singleton, captured by a singleton, or used by two queries at once, it throws "A second operation was started on this context instance before a previous operation completed", or quietly serves stale data. Fix: `AddDbContextPool` for request code, `AddPooledDbContextFactory` for anything longer-lived, and `await` each query before the next.
- **Per-request state in a pooled context.** A scoped service in the context's constructor fails with "Cannot resolve scoped service '…' from root provider" in Development. A field set for one request is still there for the next. Fix: give a pooled context only its options, and pass per-request values to the code that uses it, or register that context with `AddDbContext`.
- **N+1 queries.** A query inside a loop over another query's results. Fine on test data, slow in production. Fix: one query with `Select` or `Include`. Watch the logged SQL for the same command repeated.
- **Strings and decimals left to convention.** `nvarchar(max)` columns that can't be indexed, and money in `decimal(18,2)` with a warning in the log when the model is first used. Fix: `HasMaxLength` and `HasPrecision` in the configuration, then a migration.
- **A model change with no migration.** The app's queries fail with `SqlException: Invalid column name`, and `database update` refuses to run: "The model for context 'OrdersDbContext' has pending changes. Add a new migration before updating the database." Fix: `migrations add` after every model change, and review what it generates.
- **`migrations remove` fails with "The ConnectionString property has not been initialized".** It connects to the database to check whether the migration was applied. Fix: give it a connection string (user secrets work), or, for a migration you haven't applied anywhere, `migrations remove --force`.
- **Tracking queries for reads.** Every loaded entity is tracked, which costs memory and time for no benefit. Fix: `AsNoTracking()`, or a projection.
- **Testing with the in-memory provider.** It isn't a relational database: no SQL, no transactions, no constraints, and different query behaviour, so tests pass that would fail against SQL Server. Fix: test against a real SQL Server in a container, as the exercise does.
- **NU1903 when you add the SQL Server provider.** At the time of writing, `Microsoft.EntityFrameworkCore.SqlServer` 10.0.14 pulls in `System.Security.Cryptography.Pkcs` 9.0.11, which has a known vulnerability, and warnings-as-errors fails the build. Fix: reference a patched version (10.0.13 or later). With central package management, turn on `CentralPackageTransitivePinningEnabled` and add a `PackageVersion` for it, as the samples do.

## Exercise 07: a database for the orders app

**Goal:** give the orders app a real SQL Server database. Register the context, configure the model, create the first migration, then fix three endpoints that work but talk to the database badly: a save that isn't one unit of work, a list with an N+1 query, and a cancel that isn't written yet. The request validation and `GET /orders/{id}` are already written, so you don't need to change them.

**Where:** [`samples/exercises/07-ef-core`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/07-ef-core) in the GitHub repo. Clone the repo, then work in the `Starter` project. **The tests need [Docker](/platform/docker/) running**: they start SQL Server in a container with Testcontainers, so the first run downloads the image. You don't need to change the test setup. The [Testing](/testing/) pages will cover fixtures and Testcontainers when they're written.

**Steps:**

1. In `Starter/Program.cs`, register `OrdersDbContext` with `AddDbContextPool`, using SQL Server and the `Orders` connection string (TODO 1).
2. In `Starter/OrdersDbContext.cs`, configure the order and its lines: maximum lengths, `decimal(10,2)` for money, the status stored by name, the relationship, and the `OrderLines` table name (TODO 2).
3. From the `Starter` folder, create the first migration, then read what it generated (TODO 3):

   ```sh
   dnx dotnet-ef@10.0.14 migrations add InitialCreate
   ```

4. Back in `Program.cs`, save each order and its lines with one `SaveChangesAsync` (TODO 4), make the customer's order list a single query (TODO 5), and implement cancel through change tracking (TODO 6).

**Check your work:** the tests start red and turn green as you go.

```sh
dotnet test --project samples/exercises/07-ef-core/Starter.Tests
```

If you change the model after step 3, the tests say so: add another migration, or, since this one hasn't shipped, `migrations remove --force` and add it again.

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
