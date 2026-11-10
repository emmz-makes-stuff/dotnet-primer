using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(Orders.Tests.SqlServerFixture))]

namespace Orders.Tests;

/// <summary>
/// One SQL Server container for the whole test run: starting SQL Server is the slow part, so the
/// tests share it, and each test class gets its own database on it (see <see cref="OrdersApp"/>).
/// </summary>
/// <remarks>
/// You don't need to change anything here to do the exercise. The primer's Testing pages, still to
/// come, will cover fixtures and Testcontainers.
/// </remarks>
public sealed class SqlServerFixture : IAsyncLifetime
{
    /// <summary>Pinned, not <c>latest</c>, so the tests behave the same from one run to the next.</summary>
    public const string Image = "mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04";

    private MsSqlContainer? _container;

    /// <summary>The connection string for the server's <c>master</c> database.</summary>
    public string ConnectionString =>
        _container?.GetConnectionString() ?? throw new InvalidOperationException("The SQL Server container hasn't started.");

    public async ValueTask InitializeAsync()
    {
        try
        {
            // Build() is where Testcontainers looks for Docker, so it belongs here, inside the
            // try, not in a field initialiser.
            _container = new MsSqlBuilder(Image).Build();
            await _container.StartAsync();
        }
        catch (DockerUnavailableException e)
        {
            throw new InvalidOperationException(
                "Exercise 07's tests need a running Docker engine, because they start a real SQL Server " +
                "in a container. Start Docker and run the tests again. Setting it up: " +
                "https://dotnet-primer.emmz.dev/platform/docker/",
                e);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
