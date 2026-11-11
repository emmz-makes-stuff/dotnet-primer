# Proposal

## Why

Security & auth is the next placeholder on the learning path, after Testing. Emmz chose it as the next
change on 2026-11-11. The engineers come from .NET 3.5, where security meant Forms authentication,
Windows authentication in IIS, `<authorization>` rules in `web.config`, membership providers, a
`<machineKey>`, and connection strings in config files. None of that carries over. A migration to
.NET 10 needs token-based authentication, policy-based authorization, secrets kept out of the
repository, and the defences ASP.NET Core gives a web API.

## What Changes

- **Write the full Security & auth page** (`security`), replacing the stub. It follows the stub's
  outline, plus the three extras Emmz picked on 2026-11-11 (rate limiting, antiforgery/CSRF, and
  security headers with an OWASP Top 10 map; not CORS):
  - **Authentication:** claims and `ClaimsPrincipal`, schemes, and the 401/403 difference.
  - **JWT bearer:** validating access tokens from an identity provider (authority, audience, signing
    keys), and `dotnet user-jwts` for local development.
  - **OpenID Connect:** signing users in to a server-rendered app with the cookie and OIDC handlers.
  - **Authorization:** `[Authorize]` and `RequireAuthorization`, policies with requirements and
    handlers, resource-based checks, and a fallback policy that makes endpoints private by default.
  - **Secrets:** user secrets in development, and a secret store in production, read through
    configuration.
  - **Data protection:** what it protects, and keeping its keys across restarts and instances.
  - **HTTPS and HSTS.**
  - **Rate limiting:** the built-in middleware, partitioned per user or per client.
  - **Antiforgery:** when cookie authentication needs it, and why a bearer-token API doesn't.
  - **Security headers and OWASP:** a short OWASP Top 10 map onto ASP.NET Core's defences, and the
    headers worth adding.
  - **Coming from .NET 3.5:** a short table, after the concepts and before the Pitfalls.
  - **Pitfalls,** then **Exercise 10**.
- **Providers:** the examples use a generic OpenID Connect authority. The text names Amazon Cognito
  as one provider, and AWS Secrets Manager and Parameter Store as secret stores read through
  configuration. Their setup is left to the AWS area (Emmz, 2026-11-11).
- **A Security cheatsheet** (`cheatsheets/security`). It is linked both ways with the page, listed in
  the sidebar and on the introduction, and added to the site tests' maps.
- **Exercise 10** (`samples/exercises/10-security`): a starter, a solution and shared tests, like
  01–08. It is **standalone, with an in-memory order store, so it needs no Docker and no identity
  provider** (Emmz, 2026-11-11). The engineer secures an orders API that starts out open:
  - JWT bearer authentication, configured from settings;
  - endpoints private by default, with health left public;
  - a scope policy for placing orders, a role policy for refunds, and a resource-based check so
    customers read only their own orders.

  **The tests mint their own signed tokens** with a key they give the app's configuration. They
  check valid, missing, expired, wrongly signed and wrong-audience tokens, and each policy.
- **The samples README:** the layout and the exercise table gain 10.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- **`topic-page-content`:**
  - "Full-content pages carry no placeholder" adds the Security page and its cheatsheet.
  - New: "Security page covers authentication", "… covers authorization", "… covers secrets and data
    protection", "… covers HTTPS, rate limiting and antiforgery", "… maps OWASP risks and security
    headers", and "Security page maps .NET 3.5 habits".
- **`site-structure`:** "Cheatsheets per area" grows from ten cheatsheets to eleven.
- **`sample-exercises`:**
  - "Site describes each exercise" gains a scenario for exercise 10.
  - New: "Security exercise practises JWT bearer authentication", and "Security exercise practises
    policy-based authorization".

## Impact

- **Site:**
  - `site/src/content/docs/security/index.md`, and a new `site/src/content/docs/cheatsheets/security.md`;
  - the sidebar's Cheatsheets group in `site/astro.config.mjs`;
  - the cheatsheet cards on `site/src/content/docs/index.mdx`;
  - `site/tests/dist.test.ts` (`EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS`, `CHEATSHEETS`).
- **Samples:**
  - `samples/exercises/10-security`, and both `.slnx` files;
  - a new `PackageVersion` for `Microsoft.AspNetCore.Authentication.JwtBearer` in
    `samples/Directory.Packages.props`, at 10.0.14 to match `Microsoft.AspNetCore.Mvc.Testing`. The
    tests mint tokens with `Microsoft.IdentityModel.JsonWebTokens`, which comes with it;
  - `samples/README.md`.
- **Tooling:** none new. `dotnet user-jwts` ships with the SDK, so `docs/dependencies.md` and
  `make deps` don't change.
- **Out of scope:**
  - CORS (Emmz, 2026-11-11);
  - ASP.NET Core Identity and building a login UI, beyond a sentence saying they exist;
  - running an identity provider, and setting up Cognito, Secrets Manager or Parameter Store, which
    belong to the AWS area;
  - Windows (Negotiate) authentication, beyond its row in the .NET 3.5 table;
  - mutual TLS and certificate authentication;
  - the dev certificate's setup beyond `dotnet dev-certs https --trust`.
