using System.Net;
using System.Security.Cryptography;

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
        using var client = app.Client();

        using var response = await client.GetAsync("/catalogue", TestContext.Current.CancellationToken);

        Expect.Status(response, HttpStatusCode.Unauthorized, _letThrough);
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
