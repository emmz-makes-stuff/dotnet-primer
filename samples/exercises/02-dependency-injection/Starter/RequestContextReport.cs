namespace DependencyInjection;

/// <summary>Response body for <c>GET /request-context</c>.</summary>
/// <param name="HandlerId">The id of the <see cref="RequestContext"/> injected into the handler.</param>
/// <param name="ReporterId">The id of the <see cref="RequestContext"/> the <see cref="RequestContextReporter"/> received.</param>
public sealed record RequestContextReport(Guid HandlerId, Guid ReporterId);
