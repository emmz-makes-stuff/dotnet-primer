# Stopped: 5.1 needs Emmz's visual check

<!-- Section: 5 · Task: 5.1 (unticked) · Base: 9c09761 -->

## Done myself: exercise 10 from a clean clone

A fresh `git clone --branch change/security-content` at `9c09761`, with SDK 10.0.404:

- **Starter:** `dotnet test --project samples/exercises/10-security/Starter.Tests` gives 12 total,
  8 failed and 4 passed. Each failure names its TODO. For example:
  - `GET /catalogue returned 200 OK, expected 401 Unauthorized: a request without a valid token was
    let through. Add JWT bearer authentication (TODO 1), and a fallback policy that requires an
    authenticated user (TODO 2).`
  - `the order was stored for customer "anonymous", not the token's sub "cust-1": … (TODO 1).`
  - `POST /orders returned 201 Created, expected 403 Forbidden: … (TODO 3).`
  - `POST /orders/5/refund returned 204 NoContent, expected 403 Forbidden: … (TODO 5).`
- **Solution:** 12/12.

## The question

Do the Security & auth page and its cheatsheet look right in both light and dark schemes? Does
search find "JWT", and does the copy button work on a C# block?

## Recipe

The site preview, from the repo root:

```sh
npm --prefix site run build && npm --prefix site run preview
```

Open <http://localhost:4321/security/> and <http://localhost:4321/cheatsheets/security/>.
- Toggle the theme (top right).
- Search for `JWT`.
- Press the copy button on the `Program.cs (from the exercise)` block under "JWT bearer", then paste
  it somewhere.

The widest things are the two tables near the end of the page (OWASP Top 10, and Coming from .NET
3.5) and the long OIDC `Location` block.

- **On PASS:** tick 5.1, then the whole-change review, the change-done note, and offer
  `code-review`.
- **On FAIL:** add `## 6. Review fixes: …` to `tasks.md` with what she found.

## Next

Waiting on Emmz's answer to the question above.
