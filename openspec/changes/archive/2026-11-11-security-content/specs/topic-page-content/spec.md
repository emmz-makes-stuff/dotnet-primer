## MODIFIED Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page or cheatsheet delivered with full content SHALL NOT show the "Coming soon"
placeholder note. If one of them shows the placeholder, the site test gate SHALL fail.

#### Scenario: Every finished page is checked
- **WHEN** the site test gate runs
- **THEN** it checks for the placeholder on the ASP.NET Core entry page, the Configuration,
  Dependency Injection and Middleware pages and their three cheatsheets, the .NET 10 and `dotnet`
  CLI page with the `dotnet` CLI cheatsheet, the Logging, Tracing and Metrics pages with their three
  cheatsheets, the EF Core page with its cheatsheet, the gRPC page with its cheatsheet, the Testing
  page with its cheatsheet, and the Security & auth page with its cheatsheet

#### Scenario: Reader opens a finished page
- **WHEN** a reader opens the Dependency Injection page
- **THEN** the page has no "Coming soon" note

#### Scenario: Reader opens the finished dotnet CLI cheatsheet
- **WHEN** a reader opens the `dotnet` CLI cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens a finished Observability page
- **WHEN** a reader opens the Logging, Tracing or Metrics page, or its cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished EF Core page
- **WHEN** a reader opens the EF Core page or the EF Core cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished gRPC page
- **WHEN** a reader opens the gRPC page or the gRPC cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Testing page
- **WHEN** a reader opens the Testing page or the Testing cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Security page
- **WHEN** a reader opens the Security & auth page or the Security cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Placeholder left on a finished page is caught
- **WHEN** a page listed as full content still contains the "Coming soon" placeholder note
- **THEN** the site test gate fails and names the page

## ADDED Requirements

### Requirement: Security page covers authentication
The Security & auth page SHALL cover authentication in ASP.NET Core:
- claims, the signed-in user, and authentication schemes;
- the difference between a 401 and a 403;
- validating JWT bearer access tokens from an identity provider;
- creating development tokens with `dotnet user-jwts`;
- signing users in to a server-rendered app with OpenID Connect.

#### Scenario: Reader secures an API with tokens
- **WHEN** a reader follows the page's JWT bearer example for an API
- **THEN** the page names the settings that are validated (issuer, audience, signing keys and
  lifetime), and the API answers a request without a valid token with 401

#### Scenario: Reader calls an API without an identity provider
- **WHEN** a reader wants a token for local development
- **THEN** the page shows `dotnet user-jwts` creating one, with chosen scopes and roles, that the
  API accepts

#### Scenario: Reader signs users in to a web app
- **WHEN** a reader follows the page's OpenID Connect example
- **THEN** an unauthenticated user is redirected to the identity provider, and the app keeps the
  signed-in user in a cookie

### Requirement: Security page covers authorization
The Security & auth page SHALL cover authorization:
- requiring an authenticated user on endpoints, and opting endpoints out;
- policies built from claims, scopes and roles;
- custom requirements with their handlers;
- resource-based checks, which need the resource first;
- a fallback policy, so endpoints are private unless marked otherwise.

#### Scenario: Reader requires a scope
- **WHEN** a reader adds the page's scope policy to an endpoint
- **THEN** a signed-in caller whose token lacks the scope gets 403

#### Scenario: Reader limits users to their own data
- **WHEN** a reader follows the page's resource-based example
- **THEN** a user can read a resource they own, and gets 403 for one they don't

#### Scenario: Reader adds an endpoint and forgets to protect it
- **WHEN** an app has the page's fallback policy and a new endpoint has no authorization metadata
- **THEN** the endpoint still requires an authenticated user

### Requirement: Security page covers secrets and data protection
The Security & auth page SHALL cover keeping secrets out of source control: user secrets in
development, and a secret store in production read through configuration. It SHALL name AWS Secrets
Manager and Parameter Store as stores and leave their setup to the AWS area. It SHALL cover what data
protection protects, and why its keys must persist and be shared.

#### Scenario: Reader stores a development secret
- **WHEN** a reader follows the page's user-secrets example
- **THEN** the app reads the value through configuration, and the value is not in the repository

#### Scenario: Reader runs several instances
- **WHEN** a reader deploys an app that uses cookies or antiforgery to more than one instance, or
  restarts it
- **THEN** the page says to persist the data protection keys in shared storage, and what breaks if
  they don't

### Requirement: Security page covers HTTPS, rate limiting and antiforgery
The Security & auth page SHALL cover HTTPS redirection and HSTS, and the development certificate. It
SHALL cover the built-in rate limiter, with limits per user or per client and the status a rejected
request gets. It SHALL cover antiforgery: when cookie authentication needs it, and why an API that
takes bearer tokens doesn't.

#### Scenario: Reader turns on HSTS
- **WHEN** a reader follows the page's HTTPS example
- **THEN** the page says that HSTS is sent outside Development only, and why

#### Scenario: Caller exceeds a rate limit
- **WHEN** a caller sends more requests than the page's limiter allows in its window
- **THEN** the extra requests get 429, and the page notes that the limiter's default status is
  different unless it's set

#### Scenario: Reader posts a form with cookie authentication
- **WHEN** a reader's server-rendered form posts with the user's auth cookie
- **THEN** the page shows the antiforgery token that the post needs, and a post without it is
  rejected

### Requirement: Security page maps OWASP risks and security headers
The Security & auth page SHALL have a short table mapping the OWASP Top 10 risk categories to the
ASP.NET Core defences that address them, with links to the page's own sections where it covers them.
It SHALL name the response headers worth adding to an app, and what each one does.

#### Scenario: Reader checks an OWASP category
- **WHEN** a reader looks up broken access control in the table
- **THEN** it points to the page's authorization section and the fallback policy

#### Scenario: Reader adds security headers
- **WHEN** a reader follows the page's headers example
- **THEN** the app's responses carry each header the page names, with the value shown

### Requirement: Security page maps .NET 3.5 habits
The Security & auth page SHALL have a "Coming from .NET 3.5" section, after its concepts and before
its Pitfalls. It SHALL map these habits: Forms authentication, Windows authentication, `web.config`
authorization rules, membership and role providers, `<machineKey>`, and secrets in `web.config`.

#### Scenario: Engineer looks for Forms authentication
- **WHEN** an engineer used to Forms authentication reads the mapping
- **THEN** it points to cookie authentication with OpenID Connect against an identity provider

#### Scenario: Engineer looks for machineKey
- **WHEN** an engineer used to setting `<machineKey>` in `web.config` reads the mapping
- **THEN** it says that data protection replaces it, and links to the page's data protection section
