# Merged and live

<!-- Merged: 90799a9 fast-forwarded onto main -->

- **Merge:** with Emmz's go-ahead, `change/security-content` was fast-forwarded onto `main`
  (`8daf13a..90799a9`), as earlier changes were. The local `main` was moved with
  `git branch -f main origin/main`, with no checkout.
- **CI on `main` at `90799a9`:** `site` run 43022267515 and `samples` run 43022267573 both passed.
- **Live:**
  - `https://dotnet-primer.emmz.dev/security/` returns 200, with exercise 10, the time-limited
    protector, `RequireClaim("sub")` and the OWASP Top 10:2025 table, and no "Coming soon";
  - `/cheatsheets/security/` returns 200, with `ITimeLimitedDataProtector` and the OIDC
    `NameClaimType`;
  - the introduction links to the cheatsheet;
  - `…/tree/main/samples/exercises/10-security` now returns 200.

## Next

Offer `/opsx:archive`, and wait for a yes. Before moving the change, run
`openspec validate --specs --strict` on the synced specs. `make validate` fails once no change is
active.
