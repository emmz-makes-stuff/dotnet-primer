namespace Orders;

/// <summary>The app's log messages, as source-generated methods.</summary>
internal static partial class OrderLog
{
    // TODO 1: Add two more [LoggerMessage] methods here, each with an EventId, a Level and a
    // message template, like PaymentFailed below (see the Logging page):
    //   - OrderPlaced(ILogger logger, string orderId, int itemCount), at Information,
    //     with placeholders {OrderId} and {ItemCount};
    //   - PaymentDeclined(ILogger logger, string orderId, string reason). Choose its level
    //     carefully: was anything broken, or did the app handle it?

    // The gateway itself failed: the order couldn't be placed and someone may need to act, so
    // it's an Error. The exception parameter is logged as the entry's exception, not as a property.
    [LoggerMessage(EventId = 3, Level = LogLevel.Error,
        Message = "Payment for order {OrderId} failed")]
    public static partial void PaymentFailed(ILogger logger, string orderId, Exception exception);
}
