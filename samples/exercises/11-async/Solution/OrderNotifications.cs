using System.Threading.Channels;

namespace AsyncOrders;

/// <summary>
/// Orders waiting to be sent to the warehouse, after their request has been answered. A request
/// adds an order and moves on; <see cref="NotificationWorker"/> takes them off in the background.
/// A <see cref="Channel{T}"/> holds them: the Async &amp; performance page links to it, and this
/// exercise doesn't need more than <see cref="Add"/>.
/// It's in memory, so a crash loses whatever is queued: a real system would use a durable queue.
/// </summary>
public sealed class OrderNotifications
{
    private readonly Channel<Order> _orders = Channel.CreateUnbounded<Order>(
        new UnboundedChannelOptions { SingleReader = true });

    /// <summary>Queues the order for a notification. Never blocks: the channel is unbounded.</summary>
    /// <exception cref="InvalidOperationException">The app is stopping, so nothing will send it.</exception>
    public void Add(Order order)
    {
        // An unbounded channel only refuses an item once it's completed, and Complete does that
        // when the app stops. Say so, rather than drop the order.
        if (!_orders.Writer.TryWrite(order))
        {
            throw new InvalidOperationException($"The app is stopping, so order {order.Id} can't be queued for the warehouse.");
        }
    }

    /// <summary>Takes no more orders. <see cref="ReadAllAsync"/> ends once the queued ones are read.</summary>
    public void Complete() => _orders.Writer.TryComplete();

    /// <summary>The queued orders, as they arrive, until <see cref="Complete"/> is called and the queue is empty.</summary>
    public IAsyncEnumerable<Order> ReadAllAsync() => _orders.Reader.ReadAllAsync();
}

/// <summary>
/// Sends each queued order to the warehouse, and awaits it, so a failure is caught here and logged
/// at Error with the order id, not lost. One failure doesn't stop the next notification.
/// When the app stops, it still sends the orders already queued, for as long as the host's
/// shutdown timeout allows, and logs each one it had no time for.
/// </summary>
public sealed partial class NotificationWorker(
    OrderNotifications notifications,
    IWarehouse warehouse,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    // Cancelled only when stopping runs out of time. Not stoppingToken: the host cancels that as
    // soon as stopping starts, which would drop the queue.
    private readonly CancellationTokenSource _outOfTime = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var order in notifications.ReadAllAsync())
        {
            if (_outOfTime.IsCancellationRequested)
            {
                LogNotSent(order.Id);
                continue;
            }

            try
            {
                await warehouse.NotifyAsync(order, _outOfTime.Token);
            }
            catch (OperationCanceledException) when (_outOfTime.IsCancellationRequested)
            {
                LogNotSent(order.Id);
            }
            catch (Exception exception)
            {
                // Any failure is logged, so the worker carries on with the next order.
                LogNotifyFailed(exception, order.Id);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // No more orders: ExecuteAsync sends what's queued, then ends.
        notifications.Complete();

        // cancellationToken is cancelled when the host's shutdown timeout runs out.
        using var outOfTime = cancellationToken.Register(_outOfTime.Cancel);
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _outOfTime.Dispose();
        base.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Notifying the warehouse about order {OrderId} failed.")]
    private partial void LogNotifyFailed(Exception exception, int orderId);

    [LoggerMessage(Level = LogLevel.Error, Message = "The warehouse was never told about order {OrderId}: the app stopped first.")]
    private partial void LogNotSent(int orderId);
}
