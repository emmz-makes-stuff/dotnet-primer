---
title: "Security & auth"
description: "Authentication with JWT bearer tokens and OpenID Connect, policy-based and resource-based authorization, secrets, data protection, HTTPS and HSTS, rate limiting, antiforgery, security headers and the OWASP Top 10, and coming from .NET 3.5."
learn:
  - title: "ASP.NET Core security overview"
    url: https://learn.microsoft.com/aspnet/core/security/
  - title: "Configure JWT bearer authentication"
    url: https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication
  - title: "Configure OpenID Connect web authentication"
    url: https://learn.microsoft.com/aspnet/core/security/authentication/configure-oidc-web-authentication
  - title: "Policy-based authorization"
    url: https://learn.microsoft.com/aspnet/core/security/authorization/policies
  - title: "Safe storage of app secrets in development"
    url: https://learn.microsoft.com/aspnet/core/security/app-secrets
  - title: "ASP.NET Core Data Protection overview"
    url: https://learn.microsoft.com/aspnet/core/security/data-protection/introduction
  - title: "Enforce HTTPS in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/security/enforcing-ssl
  - title: "Rate limiting middleware"
    url: https://learn.microsoft.com/aspnet/core/performance/rate-limit
  - title: "Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks"
    url: https://learn.microsoft.com/aspnet/core/security/anti-request-forgery
---

By the end of this page you'll be able to secure an ASP.NET Core app: accept access tokens from an identity provider in an API, sign users in to a server-rendered app with OpenID Connect, decide who may do what with policies and resource checks, keep secrets out of the repository, and switch on HTTPS, rate limiting, antiforgery and security headers. You'll also know which OWASP Top 10 risks each of those defends against.

:::tip[Cheatsheet]
Keep the [Security cheatsheet](/cheatsheets/security/) open while you work.
:::

The examples are the orders API from [exercise 10](#exercise-10-secure-the-orders-api): anyone may check its health, signed-in callers may see the catalogue, placing an order needs a scope, customers read only their own orders, and only support staff may refund one.

## Authentication

Authentication answers "who is this?". Its result is a `ClaimsPrincipal`: the signed-in user, with a list of claims, each a name and a value, such as `sub: ada` or `role: support`. In a request it's `HttpContext.User`, and a minimal-API endpoint can take it as a parameter:

```csharp
app.MapGet("/me", (ClaimsPrincipal user) => user.FindFirstValue("sub"));
```

A **scheme** is one way of authenticating: a bearer token in the `Authorization` header, a cookie, a client certificate. `AddAuthentication` registers the schemes, and its options say which one to use by default. `WebApplication` adds the authentication and authorization middleware for you once their services are registered. Call `UseAuthentication` and `UseAuthorization` yourself only to put them at a particular place in the [pipeline](/aspnet-core/middleware/).

Two status codes, and they mean different things:

- **401 Unauthorized** means "who are you?": no credentials, or credentials that didn't check out. The response says how to authenticate, in `WWW-Authenticate`.
- **403 Forbidden** means "I know who you are, and no": the caller is signed in, but a policy refused them.

For apps that keep their own user accounts and passwords in a database, [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity) does that work. Most new services hand sign-in to an identity provider instead, as the rest of this page does. [Certificate authentication](https://learn.microsoft.com/aspnet/core/security/authentication/certauth) covers mutual TLS between services.

## JWT bearer

An API doesn't sign anyone in. Its callers get an access token from an identity provider, and send it with every request as `Authorization: Bearer <token>`. The token is a JWT: claims, signed by the provider. The API checks the signature and the claims, and trusts what they say.

```csharp title="Program.cs (from the exercise)"
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters.NameClaimType = "sub";
    options.TokenValidationParameters.RoleClaimType = "role";
});
```

The package is `Microsoft.AspNetCore.Authentication.JwtBearer`. The rest of the settings come from configuration, in the `Authentication:Schemes:Bearer` section:

```json title="appsettings.json"
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

With an `Authority`, the handler fetches the provider's `/.well-known/openid-configuration` and its signing keys, and keeps them up to date. Every token is then checked for:

- **the issuer** (`iss`): it must be the authority;
- **the audience** (`aud`): it must be one of `ValidAudiences`, so a token meant for another API is refused;
- **the signature**: made with one of the provider's signing keys;
- **the lifetime** (`nbf` and `exp`): it must be in date.

A request without a valid token gets a 401. These are the real responses from the exercise:

```text
GET /catalogue                      (no Authorization header)
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer

GET /catalogue                      (a token that expired at 11:00)
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer error="invalid_token", error_description="The token expired at '11/11/2026 11:00:00'"

POST /orders                        (a valid token without the orders:write scope)
HTTP/1.1 403 Forbidden
```

For Amazon Cognito, the authority is the user pool: `https://cognito-idp.<region>.amazonaws.com/<user-pool-id>`. Its access tokens carry `aud` only when the app asks for a resource binding. Otherwise the app client is in `client_id`, and audience validation fails. The user's groups are in `cognito:groups`.

### Claim names

By default, the handler renames the token's claims to long Microsoft URIs: `sub` becomes `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`, and `user.FindFirstValue("sub")` finds nothing. `MapInboundClaims = false` keeps the names the token uses.

Then tell the principal which claims hold the name and the roles. `IsInRole` and `RequireRole` look for `RoleClaimType`, which is still the long URI. With `RoleClaimType = "role"`, they find the token's `role` claims. For Cognito, use `"cognito:groups"`. `MapInboundClaims` can be set from configuration, but these two can't, so all three go in code.

### Clock skew

A token is accepted for 5 minutes after it expires, to allow for clocks that disagree. That's `TokenValidationParameters.ClockSkew`. A test that expects a just-expired token to be refused must make it expire more than 5 minutes ago, or set `ClockSkew` lower.

### Local tokens with `dotnet user-jwts`

To call the API on your machine, you don't need an identity provider. `dotnet user-jwts`, part of the SDK, creates a signing key and signed tokens for the project:

```sh
dotnet user-jwts create --name ada --scope orders:write --role support
```

```text
New JWT saved with ID '8557f629'.
Name: ada
Roles: [support]
Scopes: orders:write

Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9…
```

The first time, it adds a `UserSecretsId` to the `.csproj`, keeps the key in [user secrets](#secrets), and writes the `Bearer` section into `appsettings.Development.json`. That section has the issuer `dotnet-user-jwts`, and one audience for each URL in `Properties/launchSettings.json`. The app reads it all through configuration, like a real provider's settings. Rebuild before you call the app, because the `UserSecretsId` is compiled in. Then send the token:

```sh
curl -H "Authorization: Bearer <token>" http://localhost:5290/catalogue
```

`--output token` prints the token alone. `dotnet user-jwts list` and `dotnet user-jwts print <id> --show-all` show what you've made. The tokens last 3 months.

## OpenID Connect

A server-rendered app signs users in through the identity provider, then remembers them in a cookie. Two schemes work together: the cookie says who's signed in, and OpenID Connect handles the sign-in.

```csharp title="Program.cs"
// Package: Microsoft.AspNetCore.Authentication.OpenIdConnect
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Oidc:Authority"];
    options.ClientId = builder.Configuration["Oidc:ClientId"];
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.MapInboundClaims = false;
    options.SaveTokens = true;
    options.Scope.Add("email");
});
```

When a user who isn't signed in opens a page that needs them to be, the challenge redirects them to the provider. This is the real redirect, with a static provider configuration in place of discovery:

```text
GET /account
HTTP/1.1 302 Found
Location: https://login.example.com/oauth2/authorize?client_id=orders-web
  &redirect_uri=http%3A%2F%2Flocalhost%3A5391%2Fsignin-oidc&response_type=code
  &scope=openid%20profile%20email&code_challenge=tMW7…&code_challenge_method=S256
  &response_mode=form_post&nonce=…&state=…
```

The user signs in there. The provider posts a code back to `/signin-oidc`, the handler swaps it for tokens, and the cookie scheme signs the user in. From then on, the cookie authenticates each request. `openid` and `profile` are requested by default, and PKCE (`code_challenge`) is on. `SaveTokens` keeps the tokens in the cookie, so the app can call APIs as the user. The client secret comes from [user secrets](#secrets) or a secret store, never from `appsettings.json`.

With Cognito, the authority is the user pool URL above, and the sign-in pages are Cognito's managed login. For an intranet app on Windows, the [Negotiate handler](https://learn.microsoft.com/aspnet/core/security/authentication/windowsauth) does Windows authentication, or the identity provider federates with Active Directory.

## Authorization

Authorization answers "may they do this?". It runs after authentication, and refuses with a 401 for a caller who isn't signed in, or a 403 for one who is.

### Requiring a signed-in user

```csharp
app.MapGet("/me", (ClaimsPrincipal user) => user.FindFirstValue("sub")).RequireAuthorization();
app.MapPost("/orders/{id:int}/refund", Refund).RequireAuthorization("Refunds");
app.MapGet("/health", () => "ok").AllowAnonymous();
```

`RequireAuthorization()` needs a signed-in user, and `RequireAuthorization("Refunds")` a named policy. `MapGroup("/admin").RequireAuthorization(…)` covers every endpoint in a group. Controllers use `[Authorize]`, `[Authorize(Policy = "Refunds")]` and `[Authorize(Roles = "support")]`. `AllowAnonymous()` and `[AllowAnonymous]` let everyone in, whatever other policies apply.

### Policies

A policy is a set of requirements, registered once by name. The built-in ones check roles, claims or any condition you write:

```csharp title="Program.cs (from the exercise)"
options.AddPolicy(Policies.PlaceOrders, policy => policy.RequireAssertion(context => context.User.HasScope(Scopes.WriteOrders)));
options.AddPolicy(Policies.Refunds, policy => policy.RequireRole("support"));
```

`RequireClaim("email_verified", "true")` needs a claim with one of the given values. For scopes, it isn't enough. An OAuth provider sends every scope the token grants in one `scope` claim, separated by spaces: `"orders:read orders:write"`. `RequireClaim("scope", "orders:write")` compares the whole value, so that token is refused. Split it:

```csharp title="Scopes.cs (from the exercise)"
public static bool HasScope(this ClaimsPrincipal user, string scope) =>
    user.FindAll("scope").Any(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(scope, StringComparer.Ordinal));
```

It checks every `scope` claim, because `dotnet user-jwts` sends several scopes as an array, which becomes one claim each. Entra ID calls the claim `scp`.

### Requirements and handlers

When a rule is more than a claim check, write a requirement and a handler for it. The requirement is a marker. The handler decides, and calls `Succeed` if the user meets it:

```csharp title="OrderAccess.cs (from the exercise)"
public sealed class OrderAccessRequirement : IAuthorizationRequirement;

public sealed class OrderAccessHandler : AuthorizationHandler<OrderAccessRequirement, Order>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OrderAccessRequirement requirement, Order resource)
    {
        if (context.User.FindFirstValue("sub") == resource.CustomerId || context.User.IsInRole("support"))
        {
            context.Succeed(requirement);
        }

        // Not calling Fail() leaves the decision to other handlers; with none, the check fails.
        return Task.CompletedTask;
    }
}
```

```csharp title="Program.cs (from the exercise)"
options.AddPolicy(Policies.ReadOrder, policy => policy.AddRequirements(new OrderAccessRequirement()));
// …
builder.Services.AddSingleton<IAuthorizationHandler, OrderAccessHandler>();
```

A handler can take services through its constructor. Register it with a lifetime that suits them.

### Resource-based checks

"Customers read only their own orders" can't be checked before the endpoint runs: it needs the order. So the endpoint loads it, then asks `IAuthorizationService`:

```csharp title="Program.cs (from the exercise)"
app.MapGet("/orders/{id:int}", async (int id, ClaimsPrincipal user, OrderStore store, IAuthorizationService authorization) =>
{
    if (store.Find(id) is not { } order)
    {
        return Results.NotFound();
    }

    var allowed = await authorization.AuthorizeAsync(user, order, Policies.ReadOrder);
    return allowed.Succeeded ? Results.Ok(order) : Results.Forbid();
});
```

`Results.Forbid()` answers 403. An unknown id gets 404 before the check runs, so a caller can tell that someone else's order exists. Some APIs answer 404 for "not yours" too, to hide that. The exercise uses 403, which is clearer to learn from.

### The fallback policy

Every endpoint without authorization metadata of its own is open. Add an endpoint and forget `RequireAuthorization`, and it's public. A fallback policy turns that round:

```csharp title="Program.cs (from the exercise)"
options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
```

Now every endpoint needs a signed-in user, unless it says `AllowAnonymous`. The exercise's `/catalogue` has no authorization of its own, and still gets a 401 without a token. `AddAuthorizationBuilder().SetFallbackPolicy(…)` does the same.

## Secrets

Connection strings, client secrets and API keys don't belong in `appsettings.json`, or anywhere else in the repository. In development, keep them in **user secrets**:

```sh
dotnet user-secrets init
dotnet user-secrets set "Oidc:ClientSecret" "s3cr3t"
dotnet user-secrets list
```

They're stored outside the repository, in `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`, or `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` on Windows. The file isn't encrypted: it keeps secrets out of Git, not away from your machine's users. In Development, `WebApplication.CreateBuilder` adds them as a [configuration](/aspnet-core/configuration/) source after the `appsettings` files, so the app reads `builder.Configuration["Oidc:ClientSecret"]` as if the value were in `appsettings.json`.

In production, the secrets live in a secret store, read through a configuration provider. The app's code doesn't change: only where configuration comes from.

:::note[On AWS]
`Amazon.Extensions.Configuration.SystemsManager` reads Parameter Store parameters and Secrets Manager secrets into configuration. Setting it up, with IAM permissions, is on the [orchestration and AWS](/deploy/orchestration-and-aws/) page.
:::

## Data protection

ASP.NET Core encrypts and signs the data it hands to browsers with **data protection**: authentication cookies, antiforgery tokens, and the OpenID Connect handler's `state` and nonce. Your own code can use it too, through `IDataProtectionProvider`, for anything that leaves the server and must come back unchanged, such as a password-reset link:

```csharp
var protector = provider.CreateProtector("Orders.ResetLinks");
var token = protector.Protect(userId.ToString(CultureInfo.InvariantCulture));
var back = protector.Unprotect(token);   // throws if the token was changed
```

The keys are in a **key ring**, made and rotated automatically, every 90 days by default. By default, the ring is kept on the local disk, or only in memory if there's nowhere to write it. That's fine for one long-lived process. It breaks as soon as there's more than one:

- **every new container makes a new ring**, so after a deploy, every user's cookie is unreadable: they're signed out, and forms posted from pages served before the deploy fail their antiforgery check;
- **two instances behind a load balancer each have their own ring**, so a cookie made by one is rejected by the other, at random.

Keep the ring where every instance can reach it, and give every app that shares it the same name:

```csharp title="Program.cs"
builder.Services.AddDataProtection()
    .SetApplicationName("orders")
    .PersistKeysToFileSystem(new DirectoryInfo("/var/dpkeys"));
```

There are providers for Redis, a database and Azure Blob Storage. On AWS, `Amazon.AspNetCore.DataProtection.SSM` keeps the ring in Parameter Store.

## HTTPS and HSTS

```csharp title="Program.cs"
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
```

`UseHttpsRedirection` sends HTTP requests to HTTPS. `UseHsts` adds `Strict-Transport-Security: max-age=2592000` to HTTPS responses: for the next 30 days, the browser goes straight to HTTPS for this host, and won't let the user click past a bad certificate. That's why it's outside Development only. A browser that remembered HSTS for `localhost` would force HTTPS on every app you ever run there. `localhost` is in HSTS's `ExcludedHosts` too, so even in Production it isn't sent to `localhost`, and it's never sent over plain HTTP. `AddHsts` sets a longer `MaxAge` and `IncludeSubDomains` once you're sure.

On your machine, Kestrel serves HTTPS with the development certificate. Trust it once:

```sh
dotnet dev-certs https --trust
```

In a deployment, TLS usually ends at a load balancer, which talks plain HTTP to the app. Then the app sees every request as HTTP, from the load balancer's address: HTTPS redirection loops, and redirect URLs (such as OpenID Connect's `redirect_uri`) say `http`. The [forwarded headers middleware](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer) fixes that. `UseForwardedHeaders`, first in the pipeline and set to trust `X-Forwarded-For` and `X-Forwarded-Proto` from the load balancer, restores the original scheme and client address.

## Rate limiting

The rate limiter refuses requests beyond a limit, to protect the app from a client that sends too many, by accident or not:

```csharp title="Program.cs"
// using System.Threading.RateLimiting; using Microsoft.AspNetCore.RateLimiting;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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
// …
app.UseRateLimiter();
app.MapPost("/orders", PlaceOrder).RequireRateLimiting("per-user");
```

- **`fixed`** allows 10 requests in each 10-second window, shared by every caller.
- **`per-user`** is a token bucket for each caller: 20 requests at once, then 5 a second. The partition key is the user's `sub`, or the client's IP address for a caller who isn't signed in. `UseRateLimiter` comes after authentication, which `WebApplication` puts first, so `context.User` is set.

**Set `RejectionStatusCode`.** By default, a refused request gets **503 Service Unavailable**, which tells the caller, and your monitoring, that the app is down. With a limit of 2 a minute, four requests in a row get `200 200 503 503` by default, and `200 200 429 429` with the setting. 429 Too Many Requests is the status that means "slow down".

Behind a load balancer, `RemoteIpAddress` is the load balancer's, until [forwarded headers](#https-and-hsts) restore the client's.

## Antiforgery

A browser sends a site's cookies with every request to it, including a form post that another site's page made. So, with cookie authentication, a malicious page can post a form to your app as the signed-in user: cross-site request forgery. The defence is a token in the form that the other site can't read or guess. The app checks it against a matching cookie.

```csharp title="Program.cs"
builder.Services.AddAntiforgery();
// …
app.UseAntiforgery();

app.MapGet("/name", (HttpContext context, IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Content(
        $"""<form method="post"><input type="hidden" name="{tokens.FormFieldName}" value="{tokens.RequestToken}"><input name="name"><button>Save</button></form>""",
        "text/html");
});

app.MapPost("/name", ([FromForm] string name) => Results.Ok(name));
```

`GetAndStoreTokens` sets the `.AspNetCore.Antiforgery.<suffix>` cookie, and gives the token for the `__RequestVerificationToken` field. A minimal-API endpoint that binds a form checks the token automatically. A post without it gets **400**. In Development, the error page and the log say `Invalid anti-forgery token found when reading parameter "string name" from the request body as form.`; in Production, the body is empty. With it, the post succeeds. Razor Pages and MVC forms add the field for you. `DisableAntiforgery()` opts out an endpoint that takes forms from somewhere other than a browser.

An API that takes bearer tokens doesn't need antiforgery. The browser never adds an `Authorization` header by itself, so another site's page can't send a request with your user's token.

## Security headers

A few response headers tell the browser to be stricter with your pages. ASP.NET Core doesn't add them, but a small middleware can:

```csharp title="Program.cs"
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers.ContentSecurityPolicy = "default-src 'self'; frame-ancestors 'none'";
    await next(context);
});
```

Every response then carries:

```text
X-Content-Type-Options: nosniff
Referrer-Policy: strict-origin-when-cross-origin
Content-Security-Policy: default-src 'self'; frame-ancestors 'none'
```

- **`X-Content-Type-Options: nosniff`** stops the browser from guessing a response's type, so an uploaded text file can't run as a script.
- **`Referrer-Policy: strict-origin-when-cross-origin`** sends other sites only your origin, not the full URL with its paths and query strings.
- **`Content-Security-Policy`** says where scripts, styles and images may come from. `default-src 'self'` allows only your own origin, and blocks inline scripts. Start there, and loosen it for what the pages really need. Its **`frame-ancestors 'none'`** stops other sites from showing your pages in a frame, which is clickjacking's trick. It replaces `X-Frame-Options`.

Don't add `X-XSS-Protection`. Browsers have removed the filter it controlled, and the header is obsolete. Encoding output, which Razor does for you, and a Content Security Policy are the defences now.

### OWASP Top 10

The [OWASP Top 10:2025](https://top10.owasp.org/) lists the most common web application risks. What ASP.NET Core gives you against each:

| Risk                                       | Defence                                                                                                                                                          |
| ------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| A01 Broken Access Control                  | [Authorization](#authorization), [resource-based checks](#resource-based-checks) and [the fallback policy](#the-fallback-policy)                                 |
| A02 Security Misconfiguration              | [HTTPS and HSTS](#https-and-hsts), [security headers](#security-headers), and detailed errors in Development only                                                |
| A03 Software Supply Chain Failures         | NuGet audit, on by default in `dotnet restore`, and `dotnet list package --vulnerable`                                                                           |
| A04 Cryptographic Failures                 | [HTTPS](#https-and-hsts), [data protection](#data-protection), and [secrets](#secrets) out of the repository                                                     |
| A05 Injection                              | Parameterised queries ([EF Core](/data/ef-core/)), and output encoding in Razor                                                                                  |
| A06 Insecure Design                        | [Rate limiting](#rate-limiting), and private by default                                                                                                          |
| A07 Authentication Failures                | An identity provider, with [JWT bearer](#jwt-bearer) or [OpenID Connect](#openid-connect)                                                                        |
| A08 Software or Data Integrity Failures    | [Data protection](#data-protection) for data that comes back, and [antiforgery](#antiforgery)                                                                    |
| A09 Security Logging and Alerting Failures | Structured [logging](/observability/logging/) of sign-in failures and refusals, with alerts on them                                                              |
| A10 Mishandling of Exceptional Conditions  | The [exception handler and problem details](/aspnet-core/middleware/#exception-handling-with-problem-details), so errors fail closed and don't leak stack traces |

## Coming from .NET 3.5

| In .NET 3.5                                                                                | Now                                                                                           |
| ------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------- |
| Forms authentication: `<authentication mode="Forms">`, `FormsAuthentication.SetAuthCookie` | Cookie authentication, with [OpenID Connect](#openid-connect) against an identity provider    |
| Windows authentication in IIS                                                              | The Negotiate handler, or an identity provider that federates with Active Directory           |
| `<authorization><allow roles="…"/></authorization>` in `web.config`                        | `[Authorize]`, [policies](#policies) and [the fallback policy](#the-fallback-policy), in code |
| Membership and role providers                                                              | An identity provider, or ASP.NET Core Identity for local accounts                             |
| `<machineKey>`                                                                             | [Data protection](#data-protection), with its keys persisted and shared                       |
| Connection strings and keys in `web.config`, encrypted with `aspnet_regiis`                | [User secrets](#secrets) in development, and a secret store in production                     |
| `HttpContext.Current.User` and `Thread.CurrentPrincipal`                                   | `HttpContext.User`, or the `ClaimsPrincipal` an endpoint is given                             |
| Request validation (`ValidateRequest`)                                                     | No equivalent: encode output, and validate input                                              |

## Pitfalls

- **A token with several scopes is refused.** `RequireClaim("scope", "orders:write")` compares the whole claim, and the provider sent `"orders:read orders:write"`. Fix: split the claim on spaces, [as the exercise does](#policies).
- **Every `user.FindFirstValue("sub")` is null, or `RequireRole` always fails.** Inbound claim mapping renamed the token's claims, or `RoleClaimType` still names the long URI. Fix: `MapInboundClaims = false`, with `NameClaimType` and `RoleClaimType` set to the token's claim names.
- **An expired token still works.** It's within the 5-minute `ClockSkew`. It isn't a bug, but a test that expires a token by a few seconds fails. Fix: expire it by more than 5 minutes, or set `ClockSkew` lower.
- **Rate-limited callers see 503.** The limiter's default `RejectionStatusCode` is 503 Service Unavailable, so it looks like an outage. Fix: set it to 429.
- **Users are signed out after every deploy, or at random behind a load balancer.** The data protection keys aren't persisted or shared, so each container makes its own. Fix: `PersistKeysTo…` shared storage, and `SetApplicationName`.
- **A secret is committed in `appsettings.json`.** It's in the Git history now, even after you delete it. Fix: rotate the secret, then keep it in user secrets and a secret store.
- **The health check gets 401 after you add a fallback policy.** The load balancer has no token, and marks every instance unhealthy. Fix: `AllowAnonymous()` on the health endpoint, and on anything else that must stay public.
- **`dotnet user-jwts` tokens get 401 "The signature key was not found".** The app was built before `user-jwts` added the `UserSecretsId`, so it can't read the key. Fix: rebuild.

## Exercise 10: secure the orders API

**Goal:** secure an orders API that works, but lets anyone do anything. Validate JWT bearer tokens configured from settings, make every endpoint private by default, and add three checks: a scope to place orders, a role to refund them, and a resource check so customers read only their own orders, while support staff can read any. The store, the validation and the endpoints are already written. The tests stand in for the identity provider: they give the app an issuer, an audience and a signing key, and mint their own tokens.

**Where:** [`samples/exercises/10-security`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/10-security) in the GitHub repo. Clone the repo, then work in the `Starter` project. **This exercise needs no Docker and no identity provider**: the store is in memory, and the tests run the app in process with their own tokens.

**Steps:**

1. In `Starter/Program.cs`, add JWT bearer authentication. Leave its issuer, audiences and keys to configuration, and keep the token's claim names (TODO 1).
2. Add a fallback policy that requires a signed-in user, and mark `/health` with `AllowAnonymous()` (TODO 2).
3. Add the `PlaceOrders` policy, for tokens that grant `orders:write`, and require it on `POST /orders` (TODO 3). The scope claim can hold several scopes: write a helper that splits it.
4. Write the `ReadOrder` requirement and its handler: the order's customer, or the support role. In `GET /orders/{id}`, check it with `IAuthorizationService` once the order is loaded (TODO 4).
5. Add the `Refunds` policy, for the support role, and require it on the refund endpoint (TODO 5).

**Check your work:** the tests start red and turn green as you go.

```sh
dotnet test --project samples/exercises/10-security/Starter.Tests
```

Each failure names the TODO to look at. If a customer's order is stored for "anonymous", or under the wrong customer, check that claim mapping is off (TODO 1).

**Try the running app:** start it with `dotnet run --project samples/exercises/10-security/Starter`, and create a token in another terminal with `dotnet user-jwts create --project samples/exercises/10-security/Starter --name ada --scope orders:write`. Then call `http://localhost:5290/catalogue` with it.

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
