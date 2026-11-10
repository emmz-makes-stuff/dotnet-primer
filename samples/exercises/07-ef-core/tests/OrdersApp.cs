using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Orders.Tests;

/// <summary>
/// The app under test, on a database of its own on the shared SQL Server. It counts the SQL
/// commands and the SaveChanges calls the app makes, so the tests can check how it talks to the
/// database, not only what it answers.
/// </summary>
public sealed class OrdersApp(SqlServerFixture sql) : WebApplicationFactory<Program>
{
    private readonly SemaphoreSlim _migrateOnce = new(1, 1);
    private bool _migrated;

    /// <summary>This app's database: a fresh one for each test class.</summary>
    public string ConnectionString { get; } = new SqlConnectionStringBuilder(sql.ConnectionString)
    {
        InitialCatalog = $"orders_{Guid.NewGuid():N}",
    }.ConnectionString;

    /// <summary>Counts the SELECT commands the app sends.</summary>
    public SelectCounter Selects { get; } = new();

    /// <summary>Counts completed SaveChanges calls.</summary>
    public SaveCounter Saves { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", ConnectionString);
        builder.ConfigureTestServices(services =>
            // Adds to whatever AddDbContext configured; it doesn't register the context itself.
            services.ConfigureDbContext<OrdersDbContext>(options => options.AddInterceptors(Selects, Saves)));
    }

    /// <summary>
    /// Runs <paramref name="use"/> with a context from a new DI scope, as a request would get one.
    /// </summary>
    public async Task<T> WithContextAsync<T>(Func<OrdersDbContext, Task<T>> use)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetService<OrdersDbContext>();
        Assert.True(
            db is not null,
            "OrdersDbContext isn't registered. In Program.cs, register it with " +
            "builder.Services.AddDbContext<OrdersDbContext>(options => options.UseSqlServer(" +
            "builder.Configuration.GetConnectionString(\"Orders\"))).");
        return await use(db);
    }

    /// <summary>Applies the app's migrations to this database, once, after checking there are some.</summary>
    public async Task MigrateAsync()
    {
        await _migrateOnce.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            if (_migrated)
            {
                return;
            }

            await WithContextAsync(async db =>
            {
                Assert.True(
                    db.Database.GetMigrations().Any(),
                    "The app has no migrations yet. In the Starter folder, run " +
                    "`dnx dotnet-ef@10.0.14 migrations add InitialCreate` to create the first one from your model.");
                await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
                return true;
            });
            _migrated = true;
        }
        finally
        {
            _migrateOnce.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _migrateOnce.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>Counts the SELECT commands EF Core sends, from the moment it's reset.</summary>
public sealed class SelectCounter : DbCommandInterceptor
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Reset() => Interlocked.Exchange(ref _count, 0);

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        CountIfSelect(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        CountIfSelect(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        CountIfSelect(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        CountIfSelect(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void CountIfSelect(DbCommand command)
    {
        if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _count);
        }
    }
}

/// <summary>Counts completed SaveChanges calls, from the moment it's reset.</summary>
public sealed class SaveCounter : SaveChangesInterceptor
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Reset() => Interlocked.Exchange(ref _count, 0);

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Interlocked.Increment(ref _count);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _count);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
