# Design

## Context

- **The stub** `security/index.md` has a "What this covers" paragraph, two Learn links (the ASP.NET
  Core security overview, and "Safe storage of app secrets in development") and the "Coming soon"
  note. It's in `EXPECTED_SLUGS` and in the Learning path, after Testing. Neither it nor a cheatsheet
  is in `FULL_CONTENT_SLUGS` or `CHEATSHEETS`.
- **The conventions are set** in `site/README.md` and `samples/README.md`, as for gRPC:
  - the topic skeleton and the cheatsheet shape;
  - the exercise layout: `<Name>.Starter`, `<Name>.Solution`, and a shared `tests/` compiled into
    `Starter.Tests` and `Solution.Tests`, in both `.slnx` files;
  - central package management, xUnit v3 (ADR-0003), and warnings as errors.

  Exercise 08's `tests/OrdersApp.cs`, a `WebApplicationFactory<Program>` with helpers, is the model
  for 10's test app.
- **Decisions Emmz made** (2026-11-11): see proposal.md. In short:
  - the extras are rate limiting, antiforgery and security headers with OWASP, and not CORS;
  - exercise 10 is JWT bearer plus policies, with tokens the tests mint, and no Docker;
  - the examples use a generic authority, with AWS asides.
- **Current versions** (NuGet, 2026-11-11):
  - `Microsoft.AspNetCore.Authentication.JwtBearer` and `…OpenIdConnect` have 10.0.14 and 10.0.15
    (and 11.0.0). The repo pins `Microsoft.AspNetCore.Mvc.Testing` at 10.0.14;
  - `Microsoft.IdentityModel.JsonWebTokens` is at 8.23.0, and comes with JwtBearer.
- **The site gate goes red during the exercise sections.** The "Undescribed exercise is caught"
  check fails as soon as `10-security` exists, until the page links to it. That's the same deliberate
  red as 09 in testing-content (see D10).

## Goals / Non-Goals

**Goals:**
- An engineer finishes the page able to protect an API with tokens and policies, keep secrets out of
  the repo, and switch on the defences a web app needs.
- Every status code, header and default the page states is checked by running it, not written from
  memory: 401 vs 403, `WWW-Authenticate`, HSTS, the rate limiter's status, and antiforgery's
  rejection.
- Exercise 10 runs with no Docker and no identity provider, and fails with messages that guide.

**Non-Goals:**
- A login UI, or an app that uses cookies, in the exercise. The page covers OpenID Connect; the
  exercise is an API.
- Real AWS calls. The asides name the services and packages; there's no AWS code on the page.
- A token-issuing service. `dotnet user-jwts` and the tests' own tokens stand in for one.

## Decisions

### D1. Page shape and order

The page follows the topic skeleton, with sections in this order:
1. **Outcome.**
2. **Cheatsheet tip.**
3. **Authentication:**
   - claims, `ClaimsPrincipal` and `HttpContext.User`;
   - schemes, and `AddAuthentication`;
   - 401 (who are you?) against 403 (not allowed).
4. **JWT bearer:**
   - `AddJwtBearer` with an `Authority` and an audience, and what's validated;
   - the `Authentication:Schemes:Bearer` configuration section;
   - claim mapping (D3);
   - `ClockSkew`;
   - `dotnet user-jwts` for local tokens.
5. **OpenID Connect:** cookie plus OIDC for a server-rendered app, with the challenge redirect and
   the signed-in cookie. Amazon Cognito is named as one provider, with the authority URL's shape.
6. **Authorization:**
   - `[Authorize]` and `RequireAuthorization`, and `AllowAnonymous`;
   - policies from claims, roles and scopes (D3);
   - requirements and handlers;
   - resource-based checks through `IAuthorizationService`;
   - `SetFallbackPolicy`.
7. **Secrets:**
   - `dotnet user-secrets`, and that configuration's layering reads them;
   - a secret store in production through a configuration provider. The aside names
     `Amazon.Extensions.Configuration.SystemsManager` (Parameter Store and Secrets Manager) and
     links to the AWS area.
8. **Data protection:**
   - what uses it (cookies, antiforgery, `IDataProtector`);
   - the key ring;
   - `PersistKeysToFileSystem`, and `SetApplicationName`;
   - a sentence naming `Amazon.AspNetCore.DataProtection.SSM` for AWS.
9. **HTTPS and HSTS:**
   - `UseHttpsRedirection`, and `UseHsts` outside Development;
   - `dotnet dev-certs https --trust`;
   - TLS ending at a load balancer, and the forwarded headers it needs (one paragraph).
10. **Rate limiting:** `AddRateLimiter` with a fixed-window and a token-bucket policy, partitioned by
    user or by client IP, with `RejectionStatusCode = 429`. The default is 503, and that's a pitfall.
11. **Antiforgery:** why cookie auth is open to CSRF, `AddAntiforgery`, a minimal-API form that
    validates the token, and why bearer-token APIs don't need it.
12. **Security headers and OWASP:**
    - a middleware that adds `X-Content-Type-Options`, `Referrer-Policy`, a starting
      `Content-Security-Policy` and `frame-ancestors`;
    - that `X-XSS-Protection` is obsolete;
    - the OWASP table (D7).
13. **"Coming from .NET 3.5":** a short table (D8).
14. **Pitfalls,** then **Exercise 10**.

ASP.NET Core Identity, Windows (Negotiate) authentication and certificate authentication each get a
sentence and a Learn link, in the section they're nearest to.

### D2. Exercise 10's authentication comes from configuration

The Solution calls `AddAuthentication().AddJwtBearer()` with no code options. The handler reads
`Authentication:Schemes:Bearer`:
- `ValidIssuer`, `ValidAudiences`;
- `SigningKeys` (issuer, and base64 `Value`).

That's the section `dotnet user-jwts` writes, and the same shape a real deployment would point at an
`Authority`. So:
- **the app's `appsettings.Development.json`** has the issuer and audience that `dotnet user-jwts`
  uses for the project. The signing key lives in user secrets, which `user-jwts` creates. Both
  projects get a `UserSecretsId`, so the engineer can call the running app with a real token;
- **the tests** give the factory their own issuer, audience and a random 256-bit key per test app
  (`UseSetting`), and mint HS256 tokens with `JsonWebTokenHandler` and that key. No metadata is
  fetched, because there's no `Authority`.

The probe (1.1) confirms that binding `SigningKeys` from configuration works with no `Authority`, and
records exactly what `dotnet user-jwts create` writes and prints.

**Revised in §1** (probe note `20261111-1120`):
- **`ValidAudiences` come from `launchSettings.json`.** `user-jwts` writes every URL in the launch
  profile, so both projects ship a `launchSettings.json` with fixed ports.
- **`user-jwts` rewrites `appsettings.Development.json`** unless the file is already in its own
  format (arrays one item per line, no final newline). The exercise ships the file in exactly that
  format, so running `user-jwts` changes no tracked file.
- **The engineer must rebuild after the first `user-jwts`** if the project had no `UserSecretsId`.
  The projects ship with one, so this is a page pitfall, not an exercise step.

**Rejected:**
- **a hard-coded key in `Program.cs`**, because it teaches committing a secret;
- **an in-test OIDC metadata endpoint with RSA keys**, because it's more machinery than the lesson
  needs. The page covers `Authority` and key discovery instead.

### D3. Claims: `sub`, `role` and a space-separated `scope`

- **Tokens carry `sub`, `role` and `scope`.** `scope` is one space-separated string, as OAuth access
  tokens from Cognito and most providers send it (Entra ID calls it `scp`).
- **The Solution turns off inbound claim mapping** (`MapInboundClaims = false`), with
  `NameClaimType = "sub"` and `RoleClaimType = "role"`. Then claim names match the token, and
  `RequireRole` works. The probe checks whether that can be set from configuration, or needs one
  line of code, and what `dotnet user-jwts` emits for `--scope` and `--role`.
- **The scope policy splits the `scope` claim**, with a small helper used by `RequireAssertion`. A
  plain `RequireClaim("scope", …)` only matches a token with exactly one scope, and that's a pitfall
  the page names.

**Revised in §1** (probe note `20261111-1120`):
- **`MapInboundClaims` binds from configuration, but `NameClaimType` and `RoleClaimType` don't.**
  So TODO 1 sets all three in an `AddJwtBearer(o => …)` lambda, next to the configuration section.
  With mapping off, but `RoleClaimType` left alone, `RequireRole` fails.
- **`dotnet user-jwts` writes several scopes as a JSON array**, which becomes several `scope`
  claims. OAuth providers send one space-separated string. **The helper takes every `scope` claim
  and splits each one on spaces**, so it handles both. The tests mint the space-separated form,
  because that's what a real provider sends.

### D4. Exercise 10 shape

**The API** (in memory; the store, validation and endpoints are pre-written in both projects):
- `GET /health`: public.
- `GET /catalogue`: the SKUs on sale. It has **no authorization metadata of its own**, so it shows
  the fallback policy at work.
- `POST /orders`: places an order for the caller (`sub` becomes the order's customer id). It needs
  the `orders:write` scope.
- `GET /orders/{id}`: 404 for an unknown id. Otherwise the order's customer or the `support` role can
  read it, and anyone else gets 403.
- `POST /orders/{id}/refund`: the `support` role only. It marks the order Refunded and returns 204.

**The Starter "works, but badly"** (the 04 and 07 pattern):
- everything is open, and `POST /orders` records the customer as whatever `sub` the caller has, or
  `"anonymous"`;
- `GET /orders/{id}` returns any order to anyone.

**The Starter's TODOs:**
1. Add JWT bearer authentication, configured from settings, with claim mapping off (D3).
2. Add a fallback policy that requires an authenticated user, and mark `/health` anonymous.
3. Add the `PlaceOrders` scope policy, and require it on `POST /orders`.
4. Add the `ReadOrder` requirement and its handler (the order's customer, or support), and check it
   in `GET /orders/{id}` with `IAuthorizationService` once the order is loaded.
5. Add the `Refunds` role policy, and require it on the refund endpoint.

**The tests** (shared `tests/`, xUnit v3):
- `SecureOrdersApp`, a `WebApplicationFactory<Program>` that sets the test issuer, audience and key.
  Its `Token(sub, scopes, roles, expires, key, audience)` helper mints a token, and a `Client(token)`
  helper sets the `Authorization` header;
- **open API:** a request with no token that gets 200 fails with "a request with no token was let
  through: add JWT bearer authentication (TODO 1)". The other tests' messages name their TODO;
- **tokens:** valid → 200. Missing, expired (by 10 minutes, past the default 5-minute `ClockSkew`),
  signed with another key, and for another audience → 401;
- **policies:** each scenario in "Security exercise practises policy-based authorization", plus
  `/health` public and `/catalogue` private.

### D5. Status codes the page shows

The page and the exercise agree:
- **401** with `WWW-Authenticate: Bearer` for no token or a bad one. The page shows the
  `error="invalid_token"` description for an expired token, captured in the probe;
- **403** for a valid token that isn't allowed;
- **404** before the authorization check for an unknown order. The page notes the trade-off: some
  APIs answer 404 for "not yours" as well, so they don't reveal that an id exists. The exercise uses
  403, because it's clearer to learn from.

### D6. Cheatsheet

It follows the cheatsheet shape (back link, `##` groups of copyable blocks, minimal prose):
- JWT bearer: code and the configuration section, and `dotnet user-jwts` commands;
- cookie plus OpenID Connect;
- authorization: `[Authorize]`, `RequireAuthorization`, `AllowAnonymous`, policies (claim, role,
  scope, assertion), a requirement and handler, a resource-based check, and the fallback policy;
- `dotnet user-secrets` commands;
- data protection: persisting keys and the application name;
- HTTPS and HSTS;
- the rate limiter;
- antiforgery;
- the security-headers middleware.

### D7. The OWASP table

It uses the **current OWASP Top 10 edition**. The probe checks the edition and the category names on
owasp.org. Each row is a category, the ASP.NET Core defence, and a link to the page's section, where
the page covers it. Categories the page doesn't cover (such as supply chain, and logging) point to
the right tool or page:
- `dotnet list package --vulnerable`, and NuGet audit;
- the Logging page.

The table is short, and has no prose under each row.

**Revised in §1:** the current edition is **OWASP Top 10:2025** (`https://top10.owasp.org/`). Its
categories are listed in the probe note.

### D8. The "Coming from .NET 3.5" table

Rows, each a .NET 3.5 habit and what to do now:
- **Forms authentication** (`<authentication mode="Forms">`, `FormsAuthentication.SetAuthCookie`) →
  cookie authentication, with OpenID Connect against an identity provider;
- **Windows authentication in IIS** → the Negotiate handler, or an identity provider that federates
  with Active Directory;
- **`<authorization><allow roles=…/>`** in `web.config` → `[Authorize]`, policies and the fallback
  policy, in code;
- **membership and role providers** → an identity provider, or ASP.NET Core Identity for local
  accounts;
- **`<machineKey>`** → data protection;
- **connection strings and keys in `web.config`**, and `aspnet_regiis` encryption → user secrets,
  and a secret store;
- **`HttpContext.Current.User` and `Thread.CurrentPrincipal`** → `HttpContext.User`, or the
  `ClaimsPrincipal` an endpoint is given;
- **request validation** (`ValidateRequest`) → no equivalent: encode output, and validate input.

### D9. Packages

**`PackageVersion` entries** for `samples/Directory.Packages.props`:
- `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.14, the same patch as Mvc.Testing. If 1.2
  finds that 10.0.15 fixes a security advisory, stop and ask whether to move both.

**The test projects don't reference `Microsoft.IdentityModel.JsonWebTokens` directly.** It comes
through the app's JwtBearer reference. If the analyzers or central package management want it
direct, 1.2 adds it at the version JwtBearer brings, and says so in a devlog note.

The OpenID Connect handler (`Microsoft.AspNetCore.Authentication.OpenIdConnect`) is used only on the
page. It's compiled in the scratch project (D11), not added to the samples.

No `docs/dependencies.md` row is needed: `dotnet user-jwts` and `dotnet user-secrets` ship with the
SDK.

### D10. Section order

1. Probe and package versions (D2, D3, D5, D7, D9).
2. Exercise 10: scaffold, Solution, tests, Starter, CI.
3. The cheatsheet, then the Security & auth page.
4. The samples README.
5. The human check.

As before, the exercise comes before the page, so the page's "from the exercise" snippets are real.
**§2 lands with `SITE_TEST_EXIT:1` on purpose:** that's the undescribed-exercise check for
`10-security`. §3 turns it green. The section notes and commit bodies record it.

### D11. Code accuracy and verification

As in earlier changes:
- every snippet is compiled in a scratch project, net10.0, with warnings as errors;
- "from the exercise" snippets are copies of the real files;
- every URL on both pages is fetched.

Also, in a scratch app:
- **401, 403 and `WWW-Authenticate`:** for no token, an expired token and a missing scope, with the
  real headers pasted;
- **`dotnet user-jwts create`** with a scope and a role, and the call with that token;
- **the OpenID Connect challenge:** the redirect's `Location`, with a static configuration in place
  of discovery, so no provider is needed;
- **the rate limiter:** the 429 (and the 503 without `RejectionStatusCode`);
- **antiforgery:** a form post without its token, rejected with the status the page states;
- **HSTS:** absent in Development, and present in Production over HTTPS;
- **the headers middleware:** the response headers it adds.

## Risks / Trade-offs

- **Claim mapping differs by token source.** `user-jwts`, Cognito and the tests' tokens may name
  scopes and roles differently. → The probe records `user-jwts`' claims. The page says which claim
  Cognito uses for groups (`cognito:groups`), and how to point `RoleClaimType` at it.
- **`ClockSkew` makes expiry tests flaky.** → Expired tokens are 10 minutes old, beyond the 5-minute
  default. The page explains the skew.
- **`user-jwts` edits tracked files.** It writes the bearer section into `appsettings.Development.json`.
  → The exercise ships that section already, with the values `user-jwts` uses, so running it changes
  no tracked file. The probe checks that.
- **The OWASP edition changes.** → The table names its edition and links to it, and the periodic
  link review catches a new one.
- **Security advice dates fast.** → Every behaviour is run in the probe or the page check, not
  written from memory. Each Learn link points to the current docs.
