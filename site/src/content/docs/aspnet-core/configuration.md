---
title: Configuration
description: Layered configuration with IConfiguration and the options pattern, replacing web.config and ConfigurationManager.
learn:
  - title: Configuration in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/
  - title: Options pattern in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options
---

## What this covers

Configuration comes from layered providers: `appsettings.json`, then `appsettings.{Environment}.json`, then user secrets, environment variables and the command line, with later layers winning. This page covers reading values through `IConfiguration`, binding sections to typed classes with the **options pattern**, and validating options at startup.

A taste of the options pattern:

```csharp title="Program.cs"
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GreetingOptions>(
    builder.Configuration.GetSection(GreetingOptions.SectionName));

var app = builder.Build();

app.MapGet("/greeting", (IOptions<GreetingOptions> options) => options.Value.Message);

app.Run();

public sealed class GreetingOptions
{
    public const string SectionName = "Greeting";
    public string Message { get; set; } = "";
}
```

:::note[Coming soon]
This page is a placeholder. Full content, examples and a cheatsheet are on the way.
:::

## Exercise 01: bind configuration to options

**Goal:** serve a greeting whose text comes from configuration, so it can be changed per environment without touching code.

**Where:** [`samples/exercises/01-configuration`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/01-configuration) in the GitHub repo. Clone the repo, then work in the `Starter` project.

**Steps:**

1. Open `Starter/Program.cs` and find the `TODO` comments.
2. Bind the `Greeting` section of configuration to `GreetingOptions`.
3. Make `GET /greeting` return `GreetingOptions.Message` instead of `501 Not Implemented`.
4. Check that an environment variable (`Greeting__Message`) overrides the value in `appsettings.json`.

**Check your work:** the tests start red and turn green as you go.

```sh
dotnet test --project samples/exercises/01-configuration/Starter.Tests
```

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
