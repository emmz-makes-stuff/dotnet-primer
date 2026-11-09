---
title: "Middleware"
description: "The request pipeline: how requests flow through middleware, and how to write your own."
learn:
  - title: "ASP.NET Core middleware"
    url: https://learn.microsoft.com/aspnet/core/fundamentals/middleware/
---

## What this covers

The middleware pipeline and why order matters, built-in middleware (routing, static files, auth, exception handling), writing custom middleware, and endpoint filters.

:::note[Coming soon]
This page is a placeholder. Full content, examples and a cheatsheet are on the way.
:::

## Exercise 03: a correlation-id middleware

**Goal:** write middleware that gives every response an `X-Correlation-Id` header, reusing the caller's id when it sends one, and place it in the pipeline so that even short-circuited and error responses carry it.

**Where:** [`samples/exercises/03-middleware`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/03-middleware) in the GitHub repo. Clone the repo, then work in the `Starter` project.

**Steps:**

1. Open `Starter/CorrelationIdMiddleware.cs`. Work out the id (TODO 1), store it in `HttpContext.Items` (TODO 2), and add it to the response (TODO 3).
2. In `Starter/Program.cs`, register the middleware with `app.UseMiddleware<CorrelationIdMiddleware>()` (TODO 4). `MaintenanceModeMiddleware` is already there, and it answers `503` without calling the rest of the pipeline, so think about where yours goes.
3. Run the tests. If the error-response test still fails, read its message, then the [Pitfalls](#pitfalls) on this page.

**Check your work:** the tests start red and turn green as you go.

```sh
dotnet test --project samples/exercises/03-middleware/Starter.Tests
```

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
