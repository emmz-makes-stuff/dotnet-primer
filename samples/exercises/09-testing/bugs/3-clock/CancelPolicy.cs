namespace TestingOrders;

/// <summary>When an order can still be cancelled.</summary>
public static class CancelPolicy
{
    /// <summary>How long after it was placed an order can be cancelled, up to and including the end.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(30);

    /// <summary>Whether <paramref name="order"/> can be cancelled now, as <paramref name="time"/> tells it.</summary>
    public static bool CanCancel(Order order, TimeProvider time) => DateTimeOffset.UtcNow - order.PlacedAt <= Window; // SEEDED BUG 3: reads the system clock, not the time provider.
}
