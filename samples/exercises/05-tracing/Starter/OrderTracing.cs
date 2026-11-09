using System.Diagnostics;

namespace Orders;

/// <summary>The app's source of spans.</summary>
internal static class OrderTracing
{
    public const string SourceName = "Orders";

    // TODO 1: Add a static readonly ActivitySource named SourceName, called Source (see the
    // Tracing page). Create it once and keep it: it's what StartActivity is called on.
}
