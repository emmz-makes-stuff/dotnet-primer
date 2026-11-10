using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcOrders.Contract;

namespace GrpcOrders;

/// <summary>
/// The orders service. Grpc.Tools generates <see cref="Orders.OrdersBase"/> from
/// Protos/orders.proto, and this class overrides one method per RPC.
/// </summary>
public sealed class OrdersService(OrderStore store, IOrderTracker tracker) : Orders.OrdersBase
{
    public override Task<PlaceOrderReply> PlaceOrder(PlaceOrderRequest request, ServerCallContext context)
    {
        // A failed call is an RpcException with a status code, not a reply with an error field.
        if (request.Problems() is { Count: > 0 } problems)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(" ", problems)));
        }

        var order = store.Add(
            request.CustomerEmail,
            [.. request.Lines.Select(l => new StoredLine(l.Sku, l.Quantity, l.UnitPriceCents))]);
        return Task.FromResult(new PlaceOrderReply { OrderId = order.Id });
    }

    public override Task<Order> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        var order = store.Find(request.OrderId) ?? throw NotFound(request.OrderId);

        // Repeated fields are read-only properties: add to them, or use a collection initializer.
        var reply = new Order
        {
            Id = order.Id,
            CustomerEmail = order.CustomerEmail,
            TotalCents = order.TotalCents,
            PlacedAt = Timestamp.FromDateTimeOffset(order.PlacedAt),
        };
        reply.Lines.AddRange(order.Lines.Select(l =>
            new OrderLine { Sku = l.Sku, Quantity = l.Quantity, UnitPriceCents = l.UnitPriceCents }));
        return Task.FromResult(reply);
    }

    public override async Task TrackOrder(
        TrackOrderRequest request, IServerStreamWriter<OrderStatusUpdate> responseStream, ServerCallContext context)
    {
        if (store.Find(request.OrderId) is null)
        {
            throw NotFound(request.OrderId);
        }

        // The call's token is cancelled when the client's deadline passes or it cancels the call.
        // Pass it to everything that waits, so the work stops then, not when it would have finished.
        await foreach (var status in tracker.TrackAsync(request.OrderId, context.CancellationToken))
        {
            await responseStream.WriteAsync(
                new OrderStatusUpdate { OrderId = request.OrderId, Status = status, At = Timestamp.FromDateTime(DateTime.UtcNow) },
                context.CancellationToken);
        }

        // Returning ends the stream: the client's read loop finishes.
    }

    private static RpcException NotFound(int orderId) =>
        new(new Status(StatusCode.NotFound, $"There's no order {orderId}."));
}
