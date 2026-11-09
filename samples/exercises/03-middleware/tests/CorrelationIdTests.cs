using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Middleware.Tests;

/// <summary>
/// Shared tests for exercise 03. Compiled into both Starter.Tests and
/// Solution.Tests, so they run against whichever app the project references.
/// Against the starter they fail until its TODOs are done.
/// </summary>
public sealed class CorrelationIdTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task ResponseGetsAGeneratedCorrelationId()
    {
        using var response = await SendAsync(factory, "/hello");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var id = CorrelationId(response);
        Assert.True(
            !string.IsNullOrWhiteSpace(id),
            $"GET /hello should return an {CorrelationIdMiddleware.HeaderName} header, but it had none. " +
            "Complete CorrelationIdMiddleware (TODOs 1-3) and register it in Program.cs (TODO 4).");
    }

    [Fact]
    public async Task IncomingCorrelationIdIsEchoed()
    {
        using var response = await SendAsync(factory, "/hello", correlationId: "abc-123");

        var id = CorrelationId(response);
        Assert.True(
            id == "abc-123",
            $"A request sent with {CorrelationIdMiddleware.HeaderName}: abc-123 should get the same id back, but got \"{id}\". " +
            "Reuse the incoming header when IsAcceptable says it's usable, and only generate an id when it isn't.");
    }

    [Fact]
    public async Task ShortCircuitedResponseStillHasCorrelationId()
    {
        using var maintenance = factory.WithWebHostBuilder(host =>
            host.ConfigureAppConfiguration(config =>
                config.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Maintenance:Enabled"] = "true" })));

        using var response = await SendAsync(maintenance, "/hello");

        Assert.True(
            response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"With Maintenance:Enabled=true, GET /hello should return 503, but returned {(int)response.StatusCode}.");
        var id = CorrelationId(response);
        Assert.True(
            !string.IsNullOrWhiteSpace(id),
            $"The 503 from MaintenanceModeMiddleware should still carry an {CorrelationIdMiddleware.HeaderName} header, but it had none. " +
            "MaintenanceModeMiddleware short-circuits, so middleware registered after it never runs: " +
            "register CorrelationIdMiddleware before it.");
    }

    [Fact]
    public async Task ErrorResponseStillHasCorrelationId()
    {
        using var response = await SendAsync(factory, "/boom");

        Assert.True(
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"GET /boom throws, so it should return 500, but returned {(int)response.StatusCode}.");
        Assert.True(
            response.Content.Headers.ContentType?.MediaType == "application/problem+json",
            "The 500 should be a Problem Details response (application/problem+json) from the exception handler, " +
            $"but its content type was \"{response.Content.Headers.ContentType}\".");
        var id = CorrelationId(response);
        Assert.True(
            !string.IsNullOrWhiteSpace(id),
            $"The 500 from the exception handler should still carry an {CorrelationIdMiddleware.HeaderName} header, but it had none. " +
            "The exception handler clears the response's headers before writing the error, so a header set " +
            "directly in your middleware is lost. Set it in context.Response.OnStarting(...) instead.");
    }

    private static async Task<HttpResponseMessage> SendAsync(
        WebApplicationFactory<Program> app, string path, string? correlationId = null)
    {
        using var client = app.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Accept.ParseAdd("application/json");
        if (correlationId is not null)
        {
            request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static string? CorrelationId(HttpResponseMessage response) =>
        response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values) ? string.Join(",", values) : null;
}
