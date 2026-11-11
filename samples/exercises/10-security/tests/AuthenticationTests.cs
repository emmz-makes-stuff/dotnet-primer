using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace SecureOrders.Tests;

/// <summary>Is the caller who their token says? The app must accept its own valid tokens and nothing else.</summary>
public sealed class AuthenticationTests(SecureOrdersApp app) : IClassFixture<SecureOrdersApp>
{
    private const string _letThrough =
        "a request without a valid token was let through. Add JWT bearer authentication (TODO 1), "
        + "and a fallback policy that requires an authenticated user (TODO 2).";

    [Fact]
    public async Task ValidTokenIsAccepted()
    {
        using var client = app.Client(app.Token("cust-1"));

        using var response = await client.GetAsync("/catalogue", TestContext.Current.CancellationToken);

        Expect.Status(response, HttpStatusCode.OK,
            "a valid token was refused. Check that TODO 1 adds the JWT bearer handler and leaves its settings to the "
            + "Authentication:Schemes:Bearer section of configuration.");
    }

    [Fact]
    public async Task MissingTokenIsRefused()
    {
        // POST /orders has its own policy, so this checks the bearer handler's challenge, not the fallback
        // policy: AuthorizationTests.NewEndpointIsPrivateByDefault does that.
        using var client = app.Client();

        using var response = await client.PostAsJsonAsync(
            "/orders", new PlaceOrderRequest([new("SKU-1", 2)]), TestContext.Current.CancellationToken);

        Expect.Status(response, HttpStatusCode.Unauthorized,
            "a request without a token was let through, and placed an order. Add JWT bearer authentication (TODO 1), "
            + "and require the PlaceOrders policy on POST /orders (TODO 3).");
    }

    [Fact]
    public async Task NameIsTheTokensSub()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = app.Client(app.Token("cust-1"));

        using var response = await client.GetAsync("/me", ct);

        Expect.Status(response, HttpStatusCode.OK, "a valid token was refused on GET /me (TODO 1).");
        var name = (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("name").GetString();
        Assert.True(name == "cust-1",
            $"GET /me said the caller's name is {(name is null ? "null" : $"\"{name}\"")}, not the token's sub \"cust-1\". "
            + "Set TokenValidationParameters.NameClaimType to \"sub\" in AddJwtBearer's options (TODO 1).");
    }

    [Fact]
    public async Task ExpiredTokenIsRefused()
    {
        using var client = app.Client(app.Token("cust-1", expired: true));

        using var response = await client.GetAsync("/catalogue", TestContext.Current.CancellationToken);

        Expect.Status(response, HttpStatusCode.Unauthorized, _letThrough);
    }

    [Fact]
    public async Task TokenSignedWithAnotherKeyIsRefused()
    {
        using var client = app.Client(app.Token("cust-1", signingKey: RandomNumberGenerator.GetBytes(32)));

        using var response = await client.GetAsync("/catalogue", TestContext.Current.CancellationToken);

        Expect.Status(response, HttpStatusCode.Unauthorized, _letThrough);
    }

    [Fact]
    public async Task TokenForAnotherAudienceIsRefused()
    {
        using var client = app.Client(app.Token("cust-1", audience: "some-other-api"));

        using var response = await client.GetAsync("/catalogue", TestContext.Current.CancellationToken);

        Expect.Status(response, HttpStatusCode.Unauthorized, _letThrough);
    }

    [Fact]
    public async Task HealthStaysPublic()
    {
        using var client = app.Client();

        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Expect.Status(response, HttpStatusCode.OK,
            "load balancers call /health with no token. Mark it with AllowAnonymous(), so the fallback policy doesn't apply (TODO 2).");
    }
}
