using Grpc.Core;
using GrpcOrders.Contract;

namespace GrpcOrders;

/// <summary>
/// The orders service. Grpc.Tools generates <see cref="Orders.OrdersBase"/> from
/// Protos/orders.proto, and this class overrides one method per RPC. An RPC it doesn't override
/// answers with the status Unimplemented.
/// </summary>
public sealed class OrdersService(OrderStore store) : Orders.OrdersBase
{
    public override Task<PlaceOrderReply> PlaceOrder(PlaceOrderRequest request, ServerCallContext context)
    {
        // TODO 2: this works, but it stores any order, even one with no email or no lines. Check the
        // request first with request.Problems() (in Orders.cs). If there are any, fail the call:
        // throw an RpcException with the status code InvalidArgument, and the problems as its detail.
        var order = store.Add(
            request.CustomerEmail,
            [.. request.Lines.Select(l => new StoredLine(l.Sku, l.Quantity, l.UnitPriceCents))]);
        return Task.FromResult(new PlaceOrderReply { OrderId = order.Id });
    }

    // TODO 3: override GetOrder. Find the order in the store, and map it to the Order message:
    // the id, email, total and lines, and placed_at as a Timestamp (Timestamp.FromDateTimeOffset).
    // Repeated fields such as Lines are read-only: add to them with Add or AddRange.
    // If there's no order with that id, throw an RpcException with the status code NotFound.

    // TODO 5: once TrackOrder is in the contract (TODO 4), override it. Add IOrderTracker to this
    // class's constructor, fail with NotFound for an unknown order, then write each status from
    // tracker.TrackAsync to the response stream. Pass context.CancellationToken to TrackAsync and
    // to WriteAsync, so the tracking stops when the client's deadline passes or it cancels.
}
