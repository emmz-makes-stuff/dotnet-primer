---
title: Security cheatsheet
description: JWT bearer, cookies with OpenID Connect, policies and resource checks, secrets, data protection, HTTPS, rate limiting, antiforgery and security headers, on one page.
prev: false
next: false
learn:
  - title: Configure JWT bearer authentication
    url: https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication
  - title: Manage JSON Web Tokens in development with dotnet user-jwts
    url: https://learn.microsoft.com/aspnet/core/security/authentication/jwt-authn
  - title: Policy-based authorization
    url: https://learn.microsoft.com/aspnet/core/security/authorization/policies
  - title: Resource-based authorization
    url: https://learn.microsoft.com/aspnet/core/security/authorization/resource-based
  - title: Configure ASP.NET Core Data Protection
    url: https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview
  - title: Rate limiting middleware
    url: https://learn.microsoft.com/aspnet/core/performance/rate-limit
  - title: Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks
    url: https://learn.microsoft.com/aspnet/core/security/anti-request-forgery
---

Back to [Security & auth](/security/).

## JWT bearer

```csharp title="Program.cs"
// Package: Microsoft.AspNetCore.Authentication.JwtBearer
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.MapInboundClaims = false;                          // keep sub, role and scope as the token names them
    options.TokenValidationParameters.NameClaimType = "sub";   // these two can't be set from configuration
    options.TokenValidationParameters.RoleClaimType = "role";
});
builder.Services.AddAuthorization();
```

The handler reads the rest from `Authentication:Schemes:Bearer`:

```json title="appsettings.json: a real identity provider"
{
  "Authentication": {
    "Schemes": {
      "Bearer": {
        "Authority": "https://login.example.com/",
        "ValidAudiences": ["orders-api"]
      }
    }
  }
}
```

With an `Authority`, the signing keys come from its `/.well-known/openid-configuration`. Lifetime, issuer, audience and signature are all checked, with a 5-minute `ClockSkew`.

```json title="appsettings.Development.json: what dotnet user-jwts writes"
{
  "Authentication": {
    "Schemes": {
      "Bearer": {
        "ValidAudiences": ["http://localhost:5290"],
        "ValidIssuer": "dotnet-user-jwts"
      }
    }
  }
}
```

```sh
dotnet user-jwts create --name ada --scope orders:write   # the signing key goes to user secrets
dotnet user-jwts create --role support --output token      # print only the token
dotnet user-jwts list
dotnet user-jwts print <id> --show-all
curl -H "Authorization: Bearer $TOKEN" http://localhost:5290/catalogue
```

| Request                   | Status | Header                                              |
| ------------------------- | ------ | --------------------------------------------------- |
| No token                  | 401    | `WWW-Authenticate: Bearer`                          |
| Expired, bad key or `aud` | 401    | `WWW-Authenticate: Bearer error="invalid_token", …` |
| Valid, but not allowed    | 403    | none                                                |

## Cookie and OpenID Connect

```csharp title="Program.cs: a server-rendered app"
// Package: Microsoft.AspNetCore.Authentication.OpenIdConnect
// using Microsoft.AspNetCore.Authentication.Cookies; using Microsoft.AspNetCore.Authentication.OpenIdConnect;
// using Microsoft.IdentityModel.Protocols.OpenIdConnect;   // OpenIdConnectResponseType
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;           // who's signed in
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;         // how to sign in
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Oidc:Authority"];
    options.ClientId = builder.Configuration["Oidc:ClientId"];
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];   // user secrets, or a secret store
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.MapInboundClaims = false;
    options.SaveTokens = true;                                           // keep the tokens in the cookie
    options.Scope.Add("email");                                          // openid and profile are there already
});
```

```csharp
app.MapGet("/login", () => Results.Challenge(new() { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme]));
app.MapPost("/logout", () => Results.SignOut(
    new() { RedirectUri = "/" },
    [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));
```

## Authorization

```csharp
app.MapGet("/me", (ClaimsPrincipal user) => user.FindFirstValue("sub")).RequireAuthorization();   // signed in
app.MapPost("/orders/{id:int}/refund", Refund).RequireAuthorization("Refunds");                   // a policy
app.MapGet("/health", () => "ok").AllowAnonymous();                                                // skips every policy
var admin = app.MapGroup("/admin").RequireAuthorization("Admin");                                   // the whole group
```

```csharp title="Controllers"
[Authorize]                       // signed in
[Authorize(Policy = "Refunds")]   // a policy
[Authorize(Roles = "support")]    // a role
[AllowAnonymous]                  // wins over all of them
```

```csharp title="Program.cs: policies"
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())   // private by default
    .AddPolicy("Refunds", policy => policy.RequireRole("support"))
    .AddPolicy("Verified", policy => policy.RequireClaim("email_verified", "true"))
    .AddPolicy("PlaceOrders", policy => policy.RequireAssertion(context => context.User.HasScope("orders:write")))
    .AddPolicy("ReadOrder", policy => policy.AddRequirements(new OrderAccessRequirement()));
builder.Services.AddSingleton<IAuthorizationHandler, OrderAccessHandler>();
```

```csharp title="Scopes.cs"
// "scope": "orders:read orders:write" is one claim, so RequireClaim("scope", "orders:write") refuses it.
public static bool HasScope(this ClaimsPrincipal user, string scope) =>
    user.FindAll("scope").Any(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(scope, StringComparer.Ordinal));
```

```csharp title="A requirement and its handler"
public sealed class OrderAccessRequirement : IAuthorizationRequirement;

public sealed class OrderAccessHandler : AuthorizationHandler<OrderAccessRequirement, Order>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OrderAccessRequirement requirement, Order resource)
    {
        if (context.User.FindFirstValue("sub") == resource.CustomerId || context.User.IsInRole("support"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;   // no Succeed: the check fails, unless another handler succeeds
    }
}
```

```csharp title="A resource-based check"
app.MapGet("/orders/{id:int}", async (int id, ClaimsPrincipal user, OrderStore store, IAuthorizationService authorization) =>
{
    if (store.Find(id) is not { } order)
    {
        return Results.NotFound();
    }

    var allowed = await authorization.AuthorizeAsync(user, order, "ReadOrder");
    return allowed.Succeeded ? Results.Ok(order) : Results.Forbid();   // Forbid: 403
});
```

## User secrets

```sh
dotnet user-secrets init                                # adds a UserSecretsId to the .csproj
dotnet user-secrets set "Oidc:ClientSecret" "s3cr3t"
dotnet user-secrets list
dotnet user-secrets remove "Oidc:ClientSecret"
dotnet user-secrets clear
```

Read in Development only, after the `appsettings` files. They're kept outside the repo, in `~/.microsoft/usersecrets/<id>/secrets.json` (`%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json` on Windows), unencrypted.

## Data protection

```csharp title="Program.cs"
builder.Services.AddDataProtection()
    .SetApplicationName("orders")                                  // apps that share keys share this name
    .PersistKeysToFileSystem(new DirectoryInfo("/var/dpkeys"));    // a volume every instance can reach
```

```csharp
public sealed class ResetLinks(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("Orders.ResetLinks");   // the purpose

    public string Create(int userId) => _protector.Protect(userId.ToString(CultureInfo.InvariantCulture));
    public int Read(string token) => int.Parse(_protector.Unprotect(token), CultureInfo.InvariantCulture);   // throws if tampered with
}
```

Without persisted keys, every new container makes new keys, and the old ones' cookies and antiforgery tokens stop working. So do other instances' cookies, behind a load balancer.

## HTTPS and HSTS

```csharp title="Program.cs"
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();   // Strict-Transport-Security: max-age=2592000. Over HTTPS only, and never for localhost
}

app.UseHttpsRedirection();
```

```csharp
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});
```

```sh
dotnet dev-certs https --trust   # a trusted localhost certificate
```

## Rate limiting

```csharp title="Program.cs"
// using System.Threading.RateLimiting; using Microsoft.AspNetCore.RateLimiting;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;   // the default is 503
    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromSeconds(10);
    });
    options.AddPolicy("per-user", context => RateLimitPartition.GetTokenBucketLimiter(
        context.User.FindFirstValue("sub") ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 20,
            TokensPerPeriod = 5,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        }));
});

app.UseRateLimiter();   // after authentication, so the per-user partition sees the user
app.MapPost("/orders", PlaceOrder).RequireRateLimiting("per-user");
```

## Antiforgery

```csharp title="Program.cs: a form with cookie sign-in"
builder.Services.AddAntiforgery();
app.UseAntiforgery();

app.MapGet("/name", (HttpContext context, IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);   // sets the antiforgery cookie too
    return Results.Content(
        $"""<form method="post"><input type="hidden" name="{tokens.FormFieldName}" value="{tokens.RequestToken}"><input name="name"><button>Save</button></form>""",
        "text/html");
});

app.MapPost("/name", ([FromForm] string name) => Results.Ok(name));   // checked: 400 without the token
app.MapPost("/api/webhook", Webhook).DisableAntiforgery();             // not a browser form
```

A bearer-token API doesn't need it: the browser never sends the token on its own.

## Security headers

```csharp title="Program.cs"
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers.ContentSecurityPolicy = "default-src 'self'; frame-ancestors 'none'";   // start strict, then loosen
    await next(context);
});
```

Don't send `X-XSS-Protection`: browsers have dropped it. `frame-ancestors` replaces `X-Frame-Options`.
