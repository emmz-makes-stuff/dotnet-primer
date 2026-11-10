namespace TestingOrders;

/// <summary>The warehouse that holds an order's stock.</summary>
public interface IWarehouse
{
    /// <summary>Puts a cancelled order's lines back into stock.</summary>
    Task ReleaseAsync(int orderId, IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken);
}

/// <summary>
/// The app's stand-in for a real warehouse: it only logs. A real one would call another service,
/// which is why tests replace it.
/// </summary>
public sealed partial class LoggingWarehouse(ILogger<LoggingWarehouse> logger) : IWarehouse
{
    public Task ReleaseAsync(int orderId, IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken)
    {
        StockReleased(logger, orderId, lines.Count);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Released stock for order {OrderId}: {LineCount} lines")]
    private static partial void StockReleased(ILogger logger, int orderId, int lineCount);
}
