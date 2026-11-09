namespace DependencyInjection;

/// <summary>
/// Counts visits for the life of the app. Only useful if there is exactly one
/// instance, so it must be registered as a singleton. Thread-safe, because a
/// singleton is shared by every concurrent request.
/// </summary>
public sealed class VisitCounter
{
    private int _count;

    /// <summary>Records a visit and returns the running total.</summary>
    public int Next() => Interlocked.Increment(ref _count);
}
