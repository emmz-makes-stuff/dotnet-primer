---
title: "Dependency Injection"
description: "The built-in service container: registering services, lifetimes, and constructor injection."
learn:
  - title: "Dependency injection in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection
  - title: ".NET dependency injection"
    url: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/overview
---

## What this covers

The built-in container, service lifetimes (singleton, scoped, transient), registration patterns, constructor and primary-constructor injection, keyed services, and the mistakes that cause captive dependencies.

:::note[Coming soon]
This page is a placeholder. Full content, examples and a cheatsheet are on the way.
:::

## Exercise 02: choose the right lifetimes

**Goal:** register three services so their lifetimes behave correctly. A visit counter must keep counting for the life of the app. A per-request context must be shared within a request and fresh for the next one.

**Where:** [`samples/exercises/02-dependency-injection`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/02-dependency-injection) in the GitHub repo. Clone the repo, then work in the `Starter` project.

**Steps:**

1. Read the comments on `VisitCounter`, `RequestContext` and `RequestContextReporter` in `Starter/`. Each one says what it needs.
2. In `Starter/Program.cs`, register each service as a singleton, scoped or transient (TODOs 1–3).
3. Make `GET /visits` return the next count from `VisitCounter` (TODO 4).
4. Make `GET /request-context` return a `RequestContextReport` built from an injected `RequestContext` and `RequestContextReporter` (TODO 5).
5. Try a wrong lifetime on purpose, such as making `RequestContextReporter` a singleton, and read what the tests and the app tell you.

**Check your work:** the tests start red and turn green as you go. Each failure message names the lifetime that's wrong.

```sh
dotnet test --project samples/exercises/02-dependency-injection/Starter.Tests
```

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
