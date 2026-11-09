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
        // TODO 1: Work out the correlation id. Use the request's X-Correlation-Id
        //         header if IsAcceptable says it's usable; otherwise generate one.
        //         Hint: context.Request.Headers[HeaderName].ToString(), and
        //         Guid.NewGuid().ToString("N").

        // TODO 2: Store the id in context.Items[ItemKey], so endpoints can read it.

        // TODO 3: Add the X-Correlation-Id header to the response.
        //         Careful: the exception handler clears the response's headers
        //         before writing its error response, so a header you set here,
        //         directly, is lost on errors. Set it in a callback that runs just
        //         before the response starts:
        //         context.Response.OnStarting(() => { ...; return Task.CompletedTask; });

        await next(context);
    }

    // Don't echo arbitrary client input into response headers and logs.
    private static bool IsAcceptable(string value) =>
        value.Length is > 0 and <= 64 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
