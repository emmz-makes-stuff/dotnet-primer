namespace TestingOrders;

/// <summary>What happened when an order was cancelled.</summary>
public enum CancelOutcome
{
    Cancelled,
    NotFound,
    AlreadyCancelled,
    WindowClosed,
}

/// <summary>Places and cancels orders.</summary>
public sealed class OrderService(OrderStore store, IWarehouse warehouse, TimeProvider time)
{
    /// <summary>Prices and stores a valid order, placed now.</summary>
    public Order Place(string customerEmail, IReadOnlyList<OrderLine> lines)
    {
        var total = Pricing.Total(Pricing.Subtotal(lines));
        return store.Add(customerEmail, lines, total, time.GetUtcNow());
    }

    /// <summary>
    /// Cancels an order if its cancel window is still open, and tells the warehouse to put its stock
    /// back.
    /// </summary>
    public async Task<CancelOutcome> CancelAsync(int orderId, CancellationToken cancellationToken)
    {
        if (store.Find(orderId) is not { } order)
        {
            return CancelOutcome.NotFound;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return CancelOutcome.AlreadyCancelled;
        }

        if (!CancelPolicy.CanCancel(order, time))
        {
            return CancelOutcome.WindowClosed;
        }

        store.Update(order with { Status = OrderStatus.Cancelled });
        await warehouse.ReleaseAsync(order.Id, order.Lines, cancellationToken);
        return CancelOutcome.Cancelled;
    }
}
