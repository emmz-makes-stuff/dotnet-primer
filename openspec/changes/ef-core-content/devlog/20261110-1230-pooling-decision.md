# Decision: recommend context pooling (5.1 feedback)

<!-- Section: 6 · Before 6.1 · Base: f2a1f83 -->

5.1 isn't ticked. Emmz's feedback on the human check, verbatim: "the `#dbcontext-and-its-lifetime`
should recommend `AddDbContextPool` for better performance and scalability".

## Emmz's answers (2026-11-10)

- **Scope:** "Page, cheatsheet and exercise". Exercise 07 practises `AddDbContextPool` too: TODO 1,
  the Solution, the test hints and the specs all change.
- **Long-lived code:** `AddPooledDbContextFactory`, in place of `AddDbContextFactory`.
- **The §6 tasks as drafted:** "Build it as written", including a new test that the context really
  is pooled.

## What I told her first

Learn presents pooling as a high-performance optimisation: it saves building a context for each
request. Its one rule is that the context holds no per-request state. `OrdersDbContext` meets that
rule, and a pooled context is still one instance per request.

## Next

Task 6.1: probe pooling in a scratch project.
