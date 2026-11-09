# Code review findings (verified)

<!-- Range: f629fdb..02ad91a · code-review medium -->

The review reported 8 candidates. I checked each one against a real Kestrel or the shared tests, in
`scratchpad/review/` and `scratchpad/ex03/`.

| # | Finding | Verdict |
|---|---|---|
| F1 | The Middleware page's recommended-order `Program.cs` uses `AddAuthentication()` with no scheme, and `/orders` has `RequireAuthorization()` | **Confirmed.** GET `/orders` gives 500 `No authenticationScheme was specified, and there was no DefaultChallengeScheme found.` |
| F2 | The exercise 03 Solution registers `CorrelationIdMiddleware` *before* `UseExceptionHandler`, but the page says the exception handler comes first | **Confirmed.** With the order exception handler, then correlation id, then maintenance, all 4 tests pass. |
| F3 | The exercise 02 starter comment says an unregistered parameter breaks "the endpoint" | **Confirmed wrong.** `/visits` *and* `/request-context` both give 500 "Body was inferred…", so one bad handler breaks every endpoint. |
| F4 | Configuration page: the env-var provider "except `ASPNETCORE_`/`DOTNET_`-prefixed" | **Confirmed wrong.** With `ASPNETCORE_PRIMERPROBE=a` and `DOTNET_PRIMERPROBE2=b`, `config["ASPNETCORE_PRIMERPROBE"]` is `a` (the raw key), `config["PRIMERPROBE"]` is `a` (prefix stripped, from host config) and `config["DOTNET_PRIMERPROBE2"]` is `b`. The unprefixed provider loads everything. |
| F5 | The Configuration cheatsheet makes the same claim | **Confirmed**, same as F4. |
| F6 | `dist.test.ts` content checks pass vacuously if `.sl-markdown-content` disappears | **Valid** (by reading the code). |
| F7 | The page says the order code "is also what the diagram shows", but the diagram leaves out HSTS | **Valid** (by reading the code). |
| F8 | Unedited files are duplicated between Starter and Solution | **Not proposing a fix.** It's the same self-contained layout as exercise 01 (design.md), so each project stands alone for learners. Changing it would change the layout of every exercise. |

## Next

- **Ask Emmz** whether to add `## 9. Review fixes: code-review findings` for F1–F7, and how to fix
  F1. My recommendation is cookie auth, built into the shared framework, with a JwtBearer note.
- **Then:** build §9 with the loop, followed by the merge, push, live check and archive offers.
