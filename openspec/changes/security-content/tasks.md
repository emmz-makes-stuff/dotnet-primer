# Tasks

## 1. Probe and package versions

- [x] 1.1 Probe the setup (design D2, D3, D5, D7, D11). In scratch projects on 10.0.4xx:
  - a `dotnet new web` API with `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.14, configured
    only from `Authentication:Schemes:Bearer` with `SigningKeys` and no `Authority`. Call it with an
    HS256 token minted by `JsonWebTokenHandler`;
  - whether `MapInboundClaims`, `NameClaimType` and `RoleClaimType` can be set from configuration,
    or need code;
  - `dotnet user-jwts create --scope orders:write --role support`:
    - what it writes to user secrets and `appsettings.Development.json`, and what it prints;
    - which claims the token carries for `scope` and `role`;
    - that the API accepts it;
  - the 401 responses, with their `WWW-Authenticate` headers, for no token, an expired token, a wrong
    key and a wrong audience. The 403 for a missing scope. The default `ClockSkew`;
  - the rate limiter's default rejection status, and 429 with `RejectionStatusCode`;
  - a minimal-API form post without its antiforgery token: the status;
  - HSTS in Development and Production;
  - the current OWASP Top 10 edition and its category names, from owasp.org.

  Write a devlog note with the exact settings, headers, statuses and outputs.
- [x] 1.2 Add the JwtBearer `PackageVersion` from design D9 to `samples/Directory.Packages.props`.
  Verify that `make samples-build` and `make samples-format` pass (unsandboxed, for NuGet). **If
  restore reports a vulnerable package, or 10.0.15 fixes an advisory, stop and ask.**

## 2. Exercise 10: security

- [x] 2.1 Scaffold `samples/exercises/10-security/` like 08 (design D4):
  - `Security.Starter` and `Security.Solution` web projects, each with a `UserSecretsId` and the
    bearer section in `appsettings.Development.json` (D2);
  - the shared `tests/`;
  - `Starter.Tests` and `Solution.Tests`.

  Add them to `Primer.slnx`. Add only the Solution and its tests to `Primer.Solutions.slnx`. Verify
  with `make samples-build`.
- [x] 2.2 Implement the Solution: the pre-written store, validation and five endpoints (D4).
  - TODOs 1–5 solved: JWT bearer from configuration with claim mapping off, the fallback policy and
    an anonymous `/health`, the `PlaceOrders` scope policy, the `ReadOrder` requirement and handler,
    and the `Refunds` role policy.
  - Verify that `make samples-build` passes.
- [x] 2.3 Write the shared tests (D4): `SecureOrdersApp` with its token and client helpers, and the
  guiding message for an API that's still open.
  - Cover every scenario in the two exercise-10 requirements, with assertion messages that name the
    TODO.
  - Verify that `make samples-test` passes.
- [x] 2.4 Write the Starter: the open API that "works, but badly" (D4), and `TODO`s 1–5.
  - Verify that the Starter tests fail with the guiding messages.
  - Complete a scratch copy, confirm that its tests go green, and discard it.
  - Then complete a second scratch copy with `RequireClaim("scope", "orders:write")` in place of the
    splitting helper. Confirm that only the multi-scope test fails.
- [x] 2.5 With the Solution running, create a token with `dotnet user-jwts` and call the API with
  it, to show that the exercise works outside the tests. Check that no tracked file changed
  (`git status`). Push the branch, and check that the `samples` workflow passes on `ubuntu-latest`
  with exercise 10's tests run. Record the run ID in the section-done note.
  - `SITE_TEST_EXIT:1` is expected here (D10). Record it as a deliberate red.

## 3. Security cheatsheet and page

- [x] 3.1 Create `site/src/content/docs/cheatsheets/security.md` (design D6), with a back link and
  Learn links. Add it to the sidebar's Cheatsheets group, the introduction's cheatsheet cards and
  `EXPECTED_SLUGS`. Verify that `make site-build` passes.
- [x] 3.2 Rewrite `site/src/content/docs/security/index.md` per D1, D3, D5, D7 and D8. Cover every
  item in the six Security page requirements. Add:
  - the cheatsheet tip;
  - the Pitfalls, including:
    - a single-scope `RequireClaim`;
    - the rate limiter's default 503;
    - unpersisted data protection keys;
    - `ClockSkew`;
    - a secret committed to `appsettings.json`;
    - forgetting `AllowAnonymous` once a fallback policy is on;
  - an "Exercise 10" section in the exercise-01 format. It says that it needs no Docker and no
    identity provider, and gives the exact GitHub tree link.
- [x] 3.3 Verify the page (D11):
  - compile every snippet in a scratch project, with warnings as errors;
  - run the 401/403, `user-jwts`, OpenID Connect challenge, rate limiter, antiforgery, HSTS and
    headers examples, and paste their real output;
  - confirm that the "from the exercise" snippets match the real files.

  Record all of it in the section-done note.
- [x] 3.4 Add `security` and `cheatsheets/security` to `FULL_CONTENT_SLUGS`, and the pair to
  `CHEATSHEETS`. Fetch every URL on both pages. Verify that `make gates` shows `GATES_EXIT:0`.

## 4. Samples README

- [x] 4.1 Update `samples/README.md`: add 10 to the layout and the exercise table. Keep the Docker
  prerequisite saying that only exercise 07 needs it. Say how to try the running app with
  `dotnet user-jwts`. Verify with `make gates`.

## 5. Human check

- [x] 5.1 Run exercise 10 from a clean clone myself (the Starter fails with guiding messages, and
  the Solution passes), and report the result. Then stop for Emmz to check, with a copy-paste recipe:
  - the Security & auth page and its cheatsheet, in light and dark schemes;
  - search finds "JWT";
  - the code-block copy button on a C# block.

  On PASS, tick and continue. On FAIL, add a review-fix section.

## Workflow follow-up

- Self-review the whole change, write the change-done note, and offer the `code-review` skill.
- Merge to `main` and push, with Emmz's go-ahead, then check the live page and cheatsheet.
- Offer `/opsx:archive`. Before moving the change, run `openspec validate --specs --strict` on the
  synced specs.
