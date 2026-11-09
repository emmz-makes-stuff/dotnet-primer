namespace Middleware;

/// <summary>
/// Answers every request with 503 Service Unavailable while the configuration
/// setting <c>Maintenance:Enabled</c> is true. It short-circuits: it never
/// calls the next middleware, so nothing registered after it runs.
/// </summary>
public sealed class MaintenanceModeMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (configuration.GetValue<bool>("Maintenance:Enabled"))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "120";
            await context.Response.WriteAsync("Down for maintenance. Please try again shortly.");
            return;
        }

        await next(context);
    }
}
