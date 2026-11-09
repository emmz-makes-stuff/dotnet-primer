using System.Diagnostics;

namespace Orders;

/// <summary>
/// The app's source of spans. One static <see cref="ActivitySource"/> per component, named after
/// it; OpenTelemetry subscribes to it by that name (<c>.AddSource("Orders")</c>).
/// </summary>
internal static class OrderTracing
{
    public const string SourceName = "Orders";

    public static readonly ActivitySource Source = new(SourceName);
}
