using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DependencyInjection.Tests;

/// <summary>
/// Shared tests for exercise 02. Compiled into both Starter.Tests and
/// Solution.Tests, so they run against whichever app the project references.
/// Each test starts its own copy of the app, so a singleton's state from one
/// test can't leak into another. Against the starter they fail until its TODOs
/// are done.
/// </summary>
public sealed class LifetimeTests
{
    [Fact]
    public async Task VisitCounterKeepsCountingAcrossRequests()
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();

        var counts = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            counts.Add(await GetAsync<int>(client, "/visits"));
        }

        Assert.True(
            counts.SequenceEqual([1, 2, 3]),
            $"Three calls to GET /visits should return 1, 2, 3, but returned {string.Join(", ", counts)}. " +
            "If every call returns 1, each request is getting a new VisitCounter: it needs to live for " +
            "the whole app, so register it as a singleton.");
    }

    [Fact]
    public async Task RequestContextIsSharedWithinARequest()
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();

        var report = await GetAsync<RequestContextReport>(client, "/request-context");

        Assert.True(
            report.HandlerId == report.ReporterId,
            "Within one request, the handler and RequestContextReporter should receive the same " +
            $"RequestContext, but got {report.HandlerId} and {report.ReporterId}. A new instance " +
            "every time it's asked for means it's transient: register RequestContext as scoped.");
    }

    [Fact]
    public async Task RequestContextIsNewForEachRequest()
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();

        var first = await GetAsync<RequestContextReport>(client, "/request-context");
        var second = await GetAsync<RequestContextReport>(client, "/request-context");

        Assert.True(
            first.HandlerId != second.HandlerId,
            $"Two requests should each get their own RequestContext, but both got {first.HandlerId}. " +
            "One instance shared by every request means it's a singleton: register RequestContext as scoped.");
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string path)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"GET {path} should return 200 OK, but returned {(int)response.StatusCode} {response.StatusCode}. " +
            "Have you completed the TODOs in Program.cs?");

        var body = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        Assert.NotNull(body);
        return body;
    }
}
