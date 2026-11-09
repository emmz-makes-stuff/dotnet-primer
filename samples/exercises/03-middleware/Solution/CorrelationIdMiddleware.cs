namespace Middleware;

/// <summary>
/// Gives every response an <c>X-Correlation-Id</c> header, so a client and the
/// server's logs can refer to the same request. Reuses the caller's id when it
/// sends a sensible one; otherwise generates a new one.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>Key under which the id is stored in <see cref="HttpContext.Items"/>.</summary>
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = IsAcceptable(incoming) ? incoming : Guid.NewGuid().ToString("N");

        // Available to anything later in the pipeline (endpoints, logging).
        context.Items[ItemKey] = correlationId;

        // Set the header just before the response starts, not now. Middleware
        // later in the pipeline can reset the response: the exception handler
        // clears status, headers and body before it writes its error response.
        // OnStarting runs after all that, so the header always makes it out.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await next(context);
    }

    // Don't echo arbitrary client input into response headers and logs.
    private static bool IsAcceptable(string value) =>
        value.Length is > 0 and <= 64 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
