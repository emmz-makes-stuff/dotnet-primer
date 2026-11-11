using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SecureOrders.Tests;

/// <summary>
/// The app under test, in memory, standing in for an identity provider too: it gives the app's
/// configuration an issuer, an audience and a signing key, and mints tokens signed with that key.
/// No identity provider, metadata endpoint or Docker is needed.
/// </summary>
public sealed class SecureOrdersApp : WebApplicationFactory<Program>
{
    public const string Issuer = "https://identity.tests.example/";
    public const string Audience = "orders-api";

    // A fresh key for every test app. It never leaves the test run.
    private readonly byte[] _signingKey = RandomNumberGenerator.GetBytes(32);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Development: so neither appsettings.Development.json nor your user secrets (where
        // `dotnet user-jwts` keeps its key) mix with the tests' settings.
        builder.UseEnvironment("Testing");

        // The same section `dotnet user-jwts` writes, and that a real deployment would point at its
        // identity provider.
        builder.UseSetting("Authentication:Schemes:Bearer:ValidIssuer", Issuer);
        builder.UseSetting("Authentication:Schemes:Bearer:ValidAudiences:0", Audience);
        builder.UseSetting("Authentication:Schemes:Bearer:SigningKeys:0:Issuer", Issuer);
        builder.UseSetting("Authentication:Schemes:Bearer:SigningKeys:0:Value", Convert.ToBase64String(_signingKey));
    }

    /// <summary>
    /// A signed access token, like one an identity provider would issue.
    /// </summary>
    /// <param name="sub">The caller's id: the <c>sub</c> claim. <c>null</c> for a token without one.</param>
    /// <param name="scope">The granted scopes, space-separated, as identity providers send them.</param>
    /// <param name="roles">One <c>role</c> claim each.</param>
    /// <param name="expired">A token that expired 10 minutes ago: past the handler's 5-minute clock skew.</param>
    /// <param name="signingKey">Another key, for a token the app must not trust.</param>
    /// <param name="audience">Another audience, for a token meant for a different API.</param>
    public string Token(
        string? sub, string? scope = null, string[]? roles = null, bool expired = false, byte[]? signingKey = null, string audience = Audience)
    {
        var claims = new List<Claim>();
        if (sub is not null)
        {
            claims.Add(new Claim("sub", sub));
        }

        if (scope is not null)
        {
            claims.Add(new Claim("scope", scope));
        }

        claims.AddRange((roles ?? []).Select(role => new Claim("role", role)));

        var now = DateTime.UtcNow;
        var issued = expired ? now.AddMinutes(-70) : now;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issued,
            NotBefore = issued,
            Expires = expired ? now.AddMinutes(-10) : now.AddMinutes(30),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(signingKey ?? _signingKey), SecurityAlgorithms.HmacSha256),
        });
    }

    /// <summary>A client that sends <paramref name="token"/> as a bearer token, or no token at all.</summary>
    public HttpClient Client(string? token = null)
    {
        var client = CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        }

        return client;
    }

    /// <summary>Places a one-line order as the token's caller, and returns the stored order.</summary>
    public async Task<Order> PlaceOrderAsync(string sub, CancellationToken cancellationToken)
    {
        using var client = Client(Token(sub, scope: "orders:write"));
        using var response = await client.PostAsJsonAsync("/orders", new PlaceOrderRequest([new("SKU-1", 2)]), cancellationToken);
        Expect.Status(response, HttpStatusCode.Created, "a token with the orders:write scope must be able to place an order (TODO 3).");
        return (await response.Content.ReadFromJsonAsync<Order>(cancellationToken))!;
    }
}

/// <summary>Assertions that say which TODO to look at when the status is wrong.</summary>
public static class Expect
{
    public static void Status(HttpResponseMessage response, HttpStatusCode expected, string hint)
    {
        if (response.StatusCode != expected)
        {
            var request = response.RequestMessage!;
            Assert.Fail(
                $"{request.Method} {request.RequestUri!.PathAndQuery} returned {(int)response.StatusCode} {response.StatusCode}, "
                + $"expected {(int)expected} {expected}: {hint}");
        }
    }
}
