namespace Orders;

/// <summary>
/// The app's log messages, as source-generated methods. Each one has a fixed level, an event id,
/// and a message template whose placeholders become structured properties.
/// </summary>
internal static partial class OrderLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Order {OrderId} placed with {ItemCount} items")]
    public static partial void OrderPlaced(ILogger logger, string orderId, int itemCount);

    // A declined payment is expected and handled, so it's a Warning, not an Error.
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Payment for order {OrderId} was declined: {Reason}")]
    public static partial void PaymentDeclined(ILogger logger, string orderId, string reason);

    // The gateway itself failed: the order couldn't be placed and someone may need to act, so
    // it's an Error. The exception parameter is logged as the entry's exception, not as a property.
    [LoggerMessage(EventId = 3, Level = LogLevel.Error,
        Message = "Payment for order {OrderId} failed")]
    public static partial void PaymentFailed(ILogger logger, string orderId, Exception exception);
}
