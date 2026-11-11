# Handoff: §2 built to 2.4, uncommitted; 2.5 (user-jwts run, push, CI) next

<!-- Section: 2 · Tasks: 2.1–2.4 done (ticked, uncommitted), 2.5 open · Base: 077ff78 -->

The context reached 40% after 2.4. **Nothing in §2 is committed.** The section commits once, after
2.5 and the gates.

## Uncommitted files

- `samples/exercises/10-security/` (new):
  - **`Solution/`:**
    - `Security.Solution.csproj`, with `UserSecretsId` `dotnet-primer-10-security`, shared with the
      Starter so one `user-jwts` key works against both;
    - `Properties/launchSettings.json`: one `http` profile on `http://localhost:5290`;
    - `appsettings.json`;
    - `appsettings.Development.json`, with the bearer section in `user-jwts`' exact format:
      `ValidAudiences: ["http://localhost:5290"]` one item per line, `ValidIssuer: "dotnet-user-jwts"`,
      **and no final newline, on purpose** (D2, revised). Don't let an editor add one;
    - `Orders.cs`: the store, catalogue and validation, byte-identical to the Starter's;
    - `Scopes.cs`: the `HasScope` helper, which splits every `scope` claim on spaces;
    - `OrderAccess.cs`: the `Policies` names, plus `OrderAccessRequirement` and `OrderAccessHandler`;
    - `Program.cs`, with TODOs 1–5 solved.
  - **`Starter/`:** the same project files, `Orders.cs` and settings, and an open `Program.cs` with
    TODOs 1–5. It has no `Scopes.cs` or `OrderAccess.cs`: writing them is TODOs 3 and 4.
  - **`tests/`:**
    - `SecureOrdersApp.cs`: the factory in the `Testing` environment, with the issuer, audience and a
      random key set through `UseSetting`; `Token(...)` mints HS256 tokens with `JsonWebTokenHandler`;
      the `Client(token)` and `PlaceOrderAsync` helpers; and `Expect.Status`, which fails with the
      method, path, both statuses and a TODO hint;
    - `AuthenticationTests.cs`: 6 tests;
    - `AuthorizationTests.cs`: 6 tests.
  - **`Starter.Tests/`, `Solution.Tests/`.**
- `samples/Primer.slnx`: all four projects. `samples/Primer.Solutions.slnx`: the Solution and its
  tests. Both were added with `dotnet sln add`.
- `openspec/changes/security-content/tasks.md`: 2.1–2.4 ticked.

## What's verified

- **Solution:** 12/12, three runs in a row.
- **Starter:** 8 fail and 4 pass (a valid token, health, split scopes, an unknown order). Each
  failure has its guiding message. The no-token ones say "a request without a valid token was let
  through. Add JWT bearer authentication (TODO 1), and a fallback policy … (TODO 2)".
- **Scratch copies** in this session's scratchpad (`chk10/`; treat them as gone):
  - the Starter completed with the Solution's files: 12/12;
  - the same with `RequireClaim("scope", Scopes.WriteOrders)`: only `ScopesSentAsOneStringAreSplit`
    fails (1 of 12). That was 2.4's check;
  - extra: with claim mapping left on (a bare `AddJwtBearer()`), only
    `CustomerReadsTheirOwnOrderButNotAnothers` fails, with the claim-mapping hint. The support role
    still works with mapping on, because `role` maps to `ClaimTypes.Role`.
- **Gates:** `SAMPLES_BUILD_EXIT:0`, `SAMPLES_FORMAT_EXIT:0`, `SAMPLES_TEST_EXIT:0` (with
  `4 of 4 seeded bugs caught.` from 09). `make gates` wasn't run: `SITE_TEST_EXIT:1` is the expected
  red for an unlinked `10-security` (D10).

## Decisions this session (not elsewhere)

- **One shared `UserSecretsId` across Starter and Solution:** design D2 said "a `UserSecretsId`" for
  each, without saying whether they're the same.
- **Port 5290** for exercise 10.
- **The test app runs in a `Testing` environment**, so the engineer's real `user-jwts` key in user
  secrets (Development only) never mixes with the tests' key.
- **The customer id is `sub`**, read with `FindFirstValue("sub")`. In the Starter, with no auth, it's
  `"anonymous"`, which the ownership test's message explains.
- **`/orders/{id}` has no `RequireAuthorization`:** the fallback policy covers it, then the
  resource check runs.

## Gotchas

- `make samples-build` doesn't run the IDE naming rules; `make samples-format` does. It caught a
  private `const` without a `_` prefix (IDE1006): constants here are `_camelCase`.
- BSD `sed` has no `\b`; use `perl -pi -e`.
- Panopticon reporting fails this session: no `PANOPTICON_TOKEN`, and the saved login is for
  another server. It isn't blocking. Tell Emmz once.

## Next

- **Resume 2.5:**
  1. Run the Solution (`dotnet run --project samples/exercises/10-security/Solution`, or build and
     start the dll in the background on 5290).
  2. Create a token: `dotnet user-jwts create --project samples/exercises/10-security/Solution --name ada --scope orders:write`
     (and one with `--role support`). Call `POST /orders`, `GET /orders/{id}` and `/catalogue` with
     it, using a `.mjs` file and `node`, because the context-mode hook blocks inline `fetch`.
  3. Check `git status`: **no tracked file may change**. Especially
     `appsettings.Development.json` and the `.csproj`.
  4. Stop the server, by PID after checking `ps`.
- Then push the branch, and check that the `samples` workflow passes on `ubuntu-latest` with
  exercise 10's tests. Record the run ID. The `site` workflow will be red (D10).
- Then the §2 self-review (`git diff 077ff78` plus untracked files), the section-done note with gate
  exit lines (`SITE_TEST_EXIT:1` recorded as the deliberate red), and one commit:
  `feat(security-content): exercise 10 (§2)`.
- Then §3, task 3.1: the cheatsheet.
