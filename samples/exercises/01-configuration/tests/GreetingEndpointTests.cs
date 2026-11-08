using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Configuration.Tests;

/// <summary>
/// Shared tests for exercise 01. Compiled into both Starter.Tests and
/// Solution.Tests, so they run against whichever app the project references.
/// Against the starter they fail until its TODOs are done.
/// </summary>
public sealed class GreetingEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GreetingReturnsMessageFromAppSettings()
    {
        var body = await GetGreetingAsync(factory);

        Assert.True(
            body == "Hello from appsettings.json",
            "GET /greeting should return the message from configuration section Greeting:Message " +
            $"in appsettings.json (\"Hello from appsettings.json\"), but returned \"{body}\".");
    }

    [Fact]
    public async Task LaterConfigurationLayerOverridesAppSettings()
    {
        // Simulates a later layer, such as the environment variable Greeting__Message.
        const string overridden = "Hello from a later configuration layer";
        using var overriddenFactory = factory.WithWebHostBuilder(host =>
            host.ConfigureAppConfiguration(config =>
                config.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Greeting:Message"] = overridden })));

        var body = await GetGreetingAsync(overriddenFactory);

        Assert.True(
            body == overridden,
            "GET /greeting should reflect the last configuration layer that sets Greeting:Message " +
            $"(\"{overridden}\"), but returned \"{body}\". Read the value through IOptions<GreetingOptions> " +
            "instead of hard-coding it.");
    }

    private static async Task<string> GetGreetingAsync(WebApplicationFactory<Program> app)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = app.CreateClient();
        using var response = await client.GetAsync(new Uri("/greeting", UriKind.Relative), cancellationToken);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"GET /greeting should return 200 OK, but returned {(int)response.StatusCode} {response.StatusCode}. " +
            "Have you completed the TODOs in Program.cs?");

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
