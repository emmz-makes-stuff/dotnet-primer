using System.Diagnostics;
using System.Runtime.CompilerServices;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcOrders.Tests.Contract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServerStatus = GrpcOrders.Contract.OrderStatus;

namespace GrpcOrders.Tests;

/// <summary>
/// The app under test, in memory, with a gRPC client that calls it through the test server, so no
/// port or HTTP/2 setup is needed. The client is built from the published contract,
/// tests/Protos/orders.proto, as any other caller's would be.
/// </summary>
public sealed class OrdersApp : WebApplicationFactory<Program>
{
    private GrpcChannel? _channel;

    /// <param name="step">How long the tracker waits between status updates.</param>
    public OrdersApp(TimeSpan step) => Tracker = new RecordingTracker(step);

    /// <summary>Stands in for the app's tracker, and records what the service did with it.</summary>
    public RecordingTracker Tracker { get; }

    /// <summary>The app's order store.</summary>
    public OrderStore Store => Services.GetRequiredService<OrderStore>();

    /// <summary>A client for the orders service. Every call to it goes through the one channel.</summary>
    public Orders.OrdersClient Client()
    {
        // A channel is costly to create and is meant to be reused, like an HttpClient.
        _channel ??= GrpcChannel.ForAddress(
            Server.BaseAddress, new GrpcChannelOptions { HttpHandler = new LaterServerDeadline(Server.CreateHandler()) });
        return new Orders.OrdersClient(_channel);
    }

    /// <summary>
    /// Gives the server a deadline one second later than the client's, so the client's own timer
    /// always ends the call first, with DeadlineExceeded.
    /// </summary>
    /// <remarks>
    /// The client sends its deadline in the grpc-timeout header, and both ends enforce it. If the
    /// server's timer fires first, this in-memory test server reports the server's reset of the
    /// stream as Unavailable, so the test would fail now and then. The service still sees its token
    /// cancelled when the client's deadline passes: the client aborts the request.
    /// </remarks>
    private sealed class LaterServerDeadline(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.TryGetValues("grpc-timeout", out var values) && ToMilliseconds(values.First()) is { } ms)
            {
                request.Headers.Remove("grpc-timeout");
                request.Headers.TryAddWithoutValidation("grpc-timeout", $"{ms + 1000}m");
            }

            return base.SendAsync(request, cancellationToken);
        }

        // The header is a number of up to 8 digits and a unit: H, M, S, m (ms), u (µs) or n (ns).
        private static long? ToMilliseconds(string timeout) =>
            long.TryParse(timeout.AsSpan(0, timeout.Length - 1), out var n)
                ? timeout[^1] switch
                {
                    'H' => n * 3_600_000,
                    'M' => n * 60_000,
                    'S' => n * 1_000,
                    'm' => n,
                    'u' => n / 1_000,
                    'n' => n / 1_000_000,
                    _ => null,
                }
                : null;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IOrderTracker>();
            services.AddSingleton<IOrderTracker>(Tracker);
        });

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _channel?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// A tracker that sends the four statuses <paramref name="step"/> apart, and records how many it
/// produced and whether it saw its cancellation token cancelled.
/// </summary>
public sealed class RecordingTracker(TimeSpan step) : IOrderTracker
{
    private readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _produced;

    /// <summary>The statuses, in the order the tracker sends them.</summary>
    public static readonly ServerStatus[] Statuses =
        [ServerStatus.Placed, ServerStatus.Picked, ServerStatus.Packed, ServerStatus.Shipped];

    /// <summary>How many statuses it has produced.</summary>
    public int Produced => Volatile.Read(ref _produced);

    /// <summary>Completes when the token passed to TrackAsync is cancelled while it waits.</summary>
    public Task Cancelled => _cancelled.Task;

    public async IAsyncEnumerable<ServerStatus> TrackAsync(int orderId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < Statuses.Length; i++)
        {
            if (i > 0)
            {
                try
                {
                    await Task.Delay(step, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _cancelled.TrySetResult();
                    throw;
                }
            }

            Interlocked.Increment(ref _produced);
            yield return Statuses[i];
        }
    }
}

/// <summary>Turns the statuses an unfinished starter answers with into hints.</summary>
public static class Hints
{
    /// <summary>The hint for a call that failed because the service or the RPC isn't there yet.</summary>
    public static string? For(RpcException ex, string rpc, string todo)
    {
        if (ex.StatusCode != StatusCode.Unimplemented)
        {
            return null;
        }

        // Seen in the 1.1 probe: no endpoint is a 404; an RPC missing from the server's contract is
        // "Method is unimplemented."; an RPC in the contract but not overridden has no detail.
        if (ex.Status.Detail.Contains("HTTP status code: 404", StringComparison.Ordinal))
        {
            return "The orders service has no endpoint. In Program.cs, register gRPC with " +
                "builder.Services.AddGrpc(), and map the service with app.MapGrpcService<OrdersService>() (TODO 1).";
        }

        if (ex.Status.Detail == "Method is unimplemented.")
        {
            return $"The service's contract has no {rpc} RPC. Add it to Protos/orders.proto (TODO 4), with the " +
                "same names and field numbers as the published contract in tests/Protos/orders.proto.";
        }

        return $"OrdersService doesn't implement {rpc} yet: override it in OrdersService.cs ({todo}).";
    }

    /// <summary>Awaits a call that should succeed, failing with a hint if the RPC isn't there yet.</summary>
    public static async Task<T> Succeeds<T>(AsyncUnaryCall<T> call, string rpc, string todo)
    {
        try
        {
            return await call;
        }
        catch (RpcException ex) when (For(ex, rpc, todo) is { } hint)
        {
            Assert.Fail(hint);
            throw;
        }
    }

    /// <summary>
    /// Runs a call that should fail with <paramref name="expected"/>, and returns the exception.
    /// </summary>
    public static async Task<RpcException> Fails(
        Func<Task> call, StatusCode expected, string rpc, string todo, string because)
    {
        try
        {
            await call();
        }
        catch (RpcException ex)
        {
            if (For(ex, rpc, todo) is { } hint)
            {
                Assert.Fail(hint);
            }

            Assert.True(
                ex.StatusCode == expected,
                $"{rpc} should fail with the status code {expected}, but it failed with {ex.StatusCode} " +
                $"('{ex.Status.Detail}'). {because}");
            return ex;
        }

        Assert.Fail($"{rpc} should fail with the status code {expected}, but it succeeded. {because}");
        throw new UnreachableException();
    }
}
