namespace DependencyInjection;

/// <summary>
/// Per-request state. Every class that asks for it during one request should
/// get the same instance, and the next request a fresh one: a scoped service.
/// </summary>
public sealed class RequestContext
{
    /// <summary>Identifies this instance, so the tests can tell instances apart.</summary>
    public Guid Id { get; } = Guid.NewGuid();
}
