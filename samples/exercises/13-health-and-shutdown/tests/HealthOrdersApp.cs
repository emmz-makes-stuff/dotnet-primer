using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace HealthOrders.Tests;

/// <summary>
/// The app under test, on real Kestrel, in the test process, with a store the tests control. Each
/// test makes its own, so stopping one app doesn't touch another test.
/// </summary>
/// <remarks>
/// Kestrel, not the in-memory TestServer, because shutdown is what's under test: Kestrel stops
/// taking connections and drains the requests it has, and TestServer doesn't. The port is
/// loopback port 0, a free one; <c>UseKestrel(0)</c> would listen on localhost:5000.
/// </remarks>
public sealed class HealthOrdersApp : WebApplicationFactory<Program>
{
    public HealthOrdersApp() => UseKestrel(options => options.Listen(IPAddress.Loopback, 0));

    public OrderStore Store { get; } = new();

    /// <summary>
    /// Tells the app to stop, as a SIGTERM does, and returns a task that completes once it has.
    /// </summary>
    /// <remarks>
    /// <c>StopApplication</c>, not <c>IHost.StopAsync</c>: it fires <c>ApplicationStopping</c>
    /// first, and then the app's own <c>Run</c> stops the host, just as for a real stop signal.
    /// </remarks>
    public Task BeginStop()
    {
        var lifetime = Services.GetRequiredService<IHostApplicationLifetime>();
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lifetime.ApplicationStopped.Register(() => stopped.TrySetResult());
        lifetime.StopApplication();
        return stopped.Task;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<OrderStore>();
            services.AddSingleton(Store);
        });
    }
}
