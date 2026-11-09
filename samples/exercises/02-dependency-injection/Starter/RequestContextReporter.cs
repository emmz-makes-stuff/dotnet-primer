namespace DependencyInjection;

/// <summary>
/// A second consumer of <see cref="RequestContext"/>. It holds no state of its
/// own, so a new instance each time is fine: a transient service.
/// </summary>
public sealed class RequestContextReporter(RequestContext context)
{
    /// <summary>The id of the <see cref="RequestContext"/> this reporter was given.</summary>
    public Guid ContextId => context.Id;
}
