using Grpc.Core;
using GrpcOrders.Tests.Protos;

namespace GrpcOrders.Tests;

/// <summary>
/// Shared tests for exercise 08. Compiled into both Starter.Tests and Solution.Tests, so they run
/// against whichever app the project references. Against the starter they fail until its TODOs
/// are done.
/// </summary>
/// <remarks>
/// Each test starts its own app in memory, so the store starts empty and no Docker is needed. The
/// tests call the service as any gRPC client would, through the published contract.
/// </remarks>
public sealed class OrdersServiceTests
{
    private static readonly TimeSpan _fast = TimeSpan.FromMilliseconds(10);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Two lines: 2 × 10.24 and 3 × 4.99, so a total of 35.45.</summary>
    private static PlaceOrderRequest ValidOrder() => new()
    {
        CustomerEmail = "ada@example.com",
        Lines =
        {
            new OrderLine { Sku = "KB-101", Quantity = 2, UnitPriceCents = 1024 },
            new OrderLine { Sku = "MS-202", Quantity = 3, UnitPriceCents = 499 },
        },
    };

    [Fact]
    public async Task PlacedOrderCanBeReadBack()
    {
        await using var app = new OrdersApp(_fast);
        var client = app.Client();

        var placed = await Hints.Succeeds(client.PlaceOrderAsync(ValidOrder(), cancellationToken: Ct), "PlaceOrder", "TODO 2");
        Assert.True(placed.OrderId > 0, $"PlaceOrder should return the new order's id, but returned {placed.OrderId}.");

        var order = await Hints.Succeeds(
            client.GetOrderAsync(new GetOrderRequest { OrderId = placed.OrderId }, cancellationToken: Ct), "GetOrder", "TODO 3");
        Assert.True(order.Id == placed.OrderId, $"GetOrder should return order {placed.OrderId}, but its id is {order.Id}.");
        Assert.True(
            order.CustomerEmail == "ada@example.com",
            $"GetOrder should return the order's customer_email, but it was '{order.CustomerEmail}'.");
        Assert.True(
            order.Lines.Count == 2 && order.Lines[0].Equals(ValidOrder().Lines[0]) && order.Lines[1].Equals(ValidOrder().Lines[1]),
            $"GetOrder should return the order's 2 lines, with their SKU, quantity and unit price, but it returned {order.Lines}. " +
            "Repeated fields are read-only: add the lines with order.Lines.Add or AddRange.");
        Assert.True(order.TotalCents == 3545, $"GetOrder should return a total_cents of 3545 (35.45), but it was {order.TotalCents}.");
        Assert.True(
            order.PlacedAt is not null && (DateTimeOffset.UtcNow - order.PlacedAt.ToDateTimeOffset()).Duration() < TimeSpan.FromMinutes(1),
            $"GetOrder should return placed_at, when the order was placed, but it was '{order.PlacedAt}'. " +
            "Use Timestamp.FromDateTimeOffset(order.PlacedAt).");
    }

    public static TheoryData<string> InvalidOrders => ["no email", "a blank email", "no lines", "a quantity of 0", "a line with no SKU", "a negative price"];

    [Theory]
    [MemberData(nameof(InvalidOrders))]
    public async Task InvalidOrdersAreRejected(string problem)
    {
        await using var app = new OrdersApp(_fast);
        var request = ValidOrder();
        switch (problem)
        {
            case "no email": request.CustomerEmail = ""; break;
            case "a blank email": request.CustomerEmail = "   "; break;
            case "no lines": request.Lines.Clear(); break;
            case "a quantity of 0": request.Lines[0].Quantity = 0; break;
            case "a line with no SKU": request.Lines[1].Sku = ""; break;
            case "a negative price": request.Lines[1].UnitPriceCents = -1; break;
            default: throw new ArgumentOutOfRangeException(nameof(problem), problem, "Not a known case.");
        }

        await Hints.Fails(
            () => app.Client().PlaceOrderAsync(request, cancellationToken: Ct).ResponseAsync,
            StatusCode.InvalidArgument, "PlaceOrder", "TODO 2",
            $"An order with {problem} is invalid: check request.Problems(), and if there are any, " +
            "throw an RpcException with StatusCode.InvalidArgument.");
        Assert.True(app.Store.Count == 0, $"An order with {problem} is invalid, so it shouldn't be stored, but the store has {app.Store.Count}.");
    }

    [Fact]
    public async Task UnknownOrderIsNotFound()
    {
        await using var app = new OrdersApp(_fast);

        await Hints.Fails(
            () => app.Client().GetOrderAsync(new GetOrderRequest { OrderId = 999 }, cancellationToken: Ct).ResponseAsync,
            StatusCode.NotFound, "GetOrder", "TODO 3",
            "There's no order 999: throw an RpcException with StatusCode.NotFound.");
    }

    [Fact]
    public async Task TrackingStreamsEveryUpdate()
    {
        await using var app = new OrdersApp(_fast);
        var client = app.Client();
        var placed = await Hints.Succeeds(client.PlaceOrderAsync(ValidOrder(), cancellationToken: Ct), "PlaceOrder", "TODO 2");

        List<OrderStatusUpdate> updates;
        try
        {
            using var call = client.TrackOrder(new TrackOrderRequest { OrderId = placed.OrderId }, cancellationToken: Ct);
            updates = await ReadAll(call);
        }
        catch (RpcException ex) when (Hints.For(ex, "TrackOrder", "TODO 5") is { } hint)
        {
            Assert.Fail(hint);
            throw;
        }

        var expected = RecordingTracker.Statuses.Select(s => (OrderStatus)(int)s).ToList();
        Assert.True(
            updates.Select(u => u.Status).SequenceEqual(expected) && updates.All(u => u.OrderId == placed.OrderId),
            $"TrackOrder should stream {string.Join(", ", expected)} for order {placed.OrderId}, then end, but it streamed " +
            $"[{string.Join(", ", updates.Select(u => $"{u.Status} for order {u.OrderId}"))}]. Write each status from " +
            "tracker.TrackAsync to the response stream. If the values are missing, 0 or mixed up, check that " +
            "OrderStatusUpdate in Protos/orders.proto has the same field numbers as tests/Protos/orders.proto: " +
            "the wire carries numbers, not names.");
    }

    [Fact]
    public async Task TrackingAnUnknownOrderIsNotFound()
    {
        await using var app = new OrdersApp(_fast);

        await Hints.Fails(
            async () =>
            {
                using var call = app.Client().TrackOrder(new TrackOrderRequest { OrderId = 999 }, cancellationToken: Ct);
                await ReadAll(call);
            },
            StatusCode.NotFound, "TrackOrder", "TODO 5",
            "There's no order 999: check the store first, and throw an RpcException with StatusCode.NotFound.");
    }

    [Fact]
    public async Task DeadlineStopsTheTracking()
    {
        // Updates a second apart, and a deadline long before the second one.
        await using var app = new OrdersApp(TimeSpan.FromSeconds(1));
        var client = app.Client();
        var placed = await Hints.Succeeds(client.PlaceOrderAsync(ValidOrder(), cancellationToken: Ct), "PlaceOrder", "TODO 2");

        await Hints.Fails(
            async () =>
            {
                using var call = client.TrackOrder(
                    new TrackOrderRequest { OrderId = placed.OrderId }, deadline: DateTime.UtcNow.AddMilliseconds(300), cancellationToken: Ct);
                await ReadAll(call);
            },
            StatusCode.DeadlineExceeded, "TrackOrder", "TODO 5",
            "The client's deadline passes long before the updates finish.");

        await ExpectTrackingStopped(app, "the deadline passed");
    }

    [Fact]
    public async Task CancellingStopsTheTracking()
    {
        await using var app = new OrdersApp(TimeSpan.FromSeconds(1));
        var client = app.Client();
        var placed = await Hints.Succeeds(client.PlaceOrderAsync(ValidOrder(), cancellationToken: Ct), "PlaceOrder", "TODO 2");

        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        await Hints.Fails(
            async () =>
            {
                using var call = client.TrackOrder(new TrackOrderRequest { OrderId = placed.OrderId }, cancellationToken: cancel.Token);
                Assert.True(await call.ResponseStream.MoveNext(Ct), "TrackOrder should stream at least one update before the client cancels.");
                await cancel.CancelAsync();
                await ReadAll(call);
            },
            StatusCode.Cancelled, "TrackOrder", "TODO 5",
            "The client cancelled the call after the first update.");

        await ExpectTrackingStopped(app, "the client cancelled");
    }

    private static async Task<List<OrderStatusUpdate>> ReadAll(AsyncServerStreamingCall<OrderStatusUpdate> call)
    {
        var updates = new List<OrderStatusUpdate>();
        await foreach (var update in call.ResponseStream.ReadAllAsync(Ct))
        {
            updates.Add(update);
        }

        return updates;
    }

    private static async Task ExpectTrackingStopped(OrdersApp app, string because)
    {
        var stopped = await Task.WhenAny(app.Tracker.Cancelled, Task.Delay(TimeSpan.FromSeconds(2), Ct)) == app.Tracker.Cancelled;
        Assert.True(
            stopped,
            $"The service kept tracking the order after {because}: the tracker's cancellation token was never cancelled. " +
            "Pass context.CancellationToken to tracker.TrackAsync (and to WriteAsync), so the work stops with the call.");
        Assert.True(
            app.Tracker.Produced < RecordingTracker.Statuses.Length,
            $"The tracker produced all {app.Tracker.Produced} updates although {because} after the first.");
    }
}
