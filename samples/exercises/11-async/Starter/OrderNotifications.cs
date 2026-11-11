using System.Threading.Channels;

namespace AsyncOrders;

/// <summary>
/// Orders waiting to be sent to the warehouse, after their request has been answered. A request
/// adds an order and moves on; <see cref="NotificationWorker"/> takes them off in the background.
/// A <see cref="Channel{T}"/> holds them: the Async &amp; performance page links to it, and this
/// exercise doesn't need more than <see cref="Add"/>.
/// </summary>
public sealed class OrderNotifications
{
    private readonly Channel<Order> _orders = Channel.CreateUnbounded<Order>(
        new UnboundedChannelOptions { SingleReader = true });

    /// <summary>Queues the order for a notification. Never blocks: the channel is unbounded.</summary>
    public void Add(Order order)
    {
        // TryWrite only fails on an unbounded channel once it's completed, and this one never is.
        _ = _orders.Writer.TryWrite(order);
    }

    /// <summary>The queued orders, as they arrive, until <paramref name="cancellationToken"/> is cancelled.</summary>
    public IAsyncEnumerable<Order> ReadAllAsync(CancellationToken cancellationToken) =>
        _orders.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>
/// Sends each queued order to the warehouse, and awaits it, so a failure is caught here and logged
/// at Error with the order id, not lost. One failure doesn't stop the next notification.
/// </summary>
public sealed partial class NotificationWorker(
    OrderNotifications notifications,
    IWarehouse warehouse,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var order in notifications.ReadAllAsync(stoppingToken))
        {
            try
            {
                await warehouse.NotifyAsync(order, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // The app is stopping: that's not a failed notification.
                return;
            }
            catch (Exception exception)
            {
                // Any failure is logged, so the worker carries on with the next order.
                LogNotifyFailed(exception, order.Id);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Notifying the warehouse about order {OrderId} failed.")]
    private partial void LogNotifyFailed(Exception exception, int orderId);
}
