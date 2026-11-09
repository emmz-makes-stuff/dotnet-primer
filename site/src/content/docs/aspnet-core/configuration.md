---
title: Configuration
description: Layered configuration from JSON files, environment variables, user secrets and the command line, bound to typed, validated options.
learn:
  - title: Configuration in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/
  - title: Options pattern in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options
  - title: Use multiple environments in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/fundamentals/environments
  - title: Safe storage of app secrets in development
    url: https://learn.microsoft.com/aspnet/core/security/app-secrets
---

By the end of this page you'll know where an ASP.NET Core app's settings come from, which source wins when two set the same key, and how to override any setting per environment or per container without touching a file. You'll bind settings to typed classes, choose the right options interface, and make the app refuse to start when its configuration is wrong.

:::tip[Cheatsheet]
Keep the [Configuration cheatsheet](/cheatsheets/configuration/) open while you work.
:::

## Configuration is layered key–value pairs

To your code, configuration is one flat dictionary of string keys and string values. Keys are hierarchical, with `:` as the separator, and case-insensitive. This JSON:

```json title="appsettings.json"
{
  "Greeting": { "Message": "Hello from appsettings.json" },
  "Smtp": { "Host": "mail.internal", "Port": 587 },
  "ConnectionStrings": { "Orders": "Host=db;Database=orders" }
}
```

becomes the keys `Greeting:Message`, `Smtp:Host`, `Smtp:Port` and `ConnectionStrings:Orders`.

The dictionary is assembled from several **providers**, in order. Each later provider overrides keys set by earlier ones. `WebApplication.CreateBuilder` sets up these, **highest priority first**:

1. Command-line arguments.
2. Environment variables (except the `ASPNETCORE_`- and `DOTNET_`-prefixed ones, which configure the host).
3. User secrets, in the `Development` environment only.
4. `appsettings.{Environment}.json`.
5. `appsettings.json`.
6. Host configuration.

So you set defaults in `appsettings.json`, adjust them per environment in `appsettings.Production.json`, and override anything at deploy time with an environment variable, with no rebuild and no file edits. A key that no provider sets is simply missing: reading it gives `null`.

## Environments

The **environment** name selects which `appsettings.{Environment}.json` is loaded, and code can branch on it. It comes from the `ASPNETCORE_ENVIRONMENT` environment variable (or `--environment` on the command line):

```sh
ASPNETCORE_ENVIRONMENT=Staging dotnet run      # loads appsettings.Staging.json
```

When nothing sets it, the environment is **`Production`**. `dotnet run` and Visual Studio use `Properties/launchSettings.json`, which sets `Development` for local work. That file is never used in a deployed app.

```csharp title="Program.cs"
if (app.Environment.IsDevelopment())
{
    // Development-only endpoints, seed data…
}
```

Keep the number of environments small (`Development`, `Staging`, `Production`) and put differences in configuration, not in `if` branches.

## Environment variables and the command line

Environment variables can't use `:` portably, so configuration uses a **double underscore** instead. `__` in a variable name becomes `:` in the key:

```sh
Greeting__Message="Hello from the environment" dotnet run
# Greeting:Message is now "Hello from the environment"
```

This is how containers, ECS task definitions and Kubernetes manifests configure .NET apps. Array items use their index: `Smtp__Recipients__0`. Connection strings use the `ConnectionStrings` section: `ConnectionStrings__Orders`.

Command-line arguments come last, so they win over everything:

```sh
dotnet run -- --Greeting:Message="Hello from the command line"
```

## User secrets for local development

Don't put passwords or API keys in `appsettings*.json`, because those files are committed. For local development, **user secrets** keep them in your user profile, outside the repo:

```sh
dotnet user-secrets init
dotnet user-secrets set "Smtp:Password" "local-dev-password"
```

`init` adds a `UserSecretsId` to the project file. In the `Development` environment the values are loaded like any other provider, under the same keys. User secrets aren't encrypted, and they don't exist on a server. In deployed environments, supply secrets through environment variables injected by the platform, or a secret store such as AWS Secrets Manager.

## Reading values with `IConfiguration`

`builder.Configuration` (and `app.Configuration`) is the assembled `IConfiguration`. You can also inject `IConfiguration` anywhere:

```csharp title="Program.cs"
var builder = WebApplication.CreateBuilder(args);

string? message = builder.Configuration["Greeting:Message"];
int pageSize = builder.Configuration.GetValue("Paging:PageSize", defaultValue: 20);
string? orders = builder.Configuration.GetConnectionString("Orders");
IConfigurationSection smtp = builder.Configuration.GetSection("Smtp");

var app = builder.Build();

app.MapGet("/", () => $"{message} / page size {pageSize} / smtp host {smtp["Host"]}");

app.Run();
```

This is fine for one or two values at startup. Scattering string keys through your code isn't: typos fail silently, as `null`, and nothing documents what settings exist. For anything more, use the options pattern.

## The options pattern

The **options pattern** binds a configuration section to a plain class, then injects that class through [dependency injection](/aspnet-core/dependency-injection/):

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

Each property binds from the key with the same name in the section (`Greeting:Message` → `Message`), converting from strings to `int`, `bool`, `TimeSpan`, enums, arrays and nested classes. Keep options classes plain: a public parameterless constructor, public `{ get; set; }` properties, and defaults as property initializers. A `SectionName` constant keeps the key in one place.

## `IOptions`, `IOptionsSnapshot` or `IOptionsMonitor`

You ask for options through one of three interfaces. They differ in when they read configuration:

```csharp title="Program.cs"
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FeatureOptions>(builder.Configuration.GetSection("Features"));

var app = builder.Build();

app.MapGet("/once", (IOptions<FeatureOptions> o) => o.Value.NewCheckout);
app.MapGet("/per-request", (IOptionsSnapshot<FeatureOptions> o) => o.Value.NewCheckout);
app.MapGet("/live", (IOptionsMonitor<FeatureOptions> o) => o.CurrentValue.NewCheckout);

app.Run();

public sealed class FeatureOptions
{
    public bool NewCheckout { get; set; }
}
```

- **`IOptions<T>`** binds once, the first time it's used, and never changes. It's a singleton, so it's safe to inject anywhere. This is the default choice.
- **`IOptionsSnapshot<T>`** is recomputed once per request, so an edit to `appsettings.json` shows up on the next request. It's **scoped**, so you can't inject it into a singleton (see [captive dependencies](/aspnet-core/dependency-injection/#captive-dependencies)).
- **`IOptionsMonitor<T>`** is a singleton that always returns the latest value through `CurrentValue`, and can notify you with `OnChange`. Use it for long-lived services that must react to changes.

Only providers that reload see changes. The JSON files reload when edited. Environment variables and the command line are read once at startup.

## Validate options at startup

Binding never fails by itself. A misspelt section name, or a missing setting, just leaves the property at its default, and you find out on the first request that needs it, possibly in production. Validation turns that into a startup failure:

```csharp title="Program.cs"
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<SmtpOptions>()
    .BindConfiguration(SmtpOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(o => o.Port is 25 or 465 or 587, "Smtp:Port must be 25, 465 or 587.")
    .ValidateOnStart();

var app = builder.Build();

app.MapGet("/smtp", (IOptions<SmtpOptions> options) => $"{options.Value.Host}:{options.Value.Port}");

app.Run();

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; set; } = "";

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public string? UserName { get; set; }
}
```

- **`ValidateDataAnnotations()`** checks the `[Required]`, `[Range]` and other attributes on the class.
- **`Validate(…)`** adds any rule you can write as a predicate.
- **`ValidateOnStart()`** runs validation as the app starts, not on first use.

With no `Smtp:Host` configured, the app stops before it listens, with an error that names the setting:

```text
OptionsValidationException: DataAnnotation validation failed for 'SmtpOptions' members:
'Host' with the error: 'The Host field is required.'.
```

A deploy with broken configuration then fails its health check and never takes traffic.

## Pitfalls

- **Colons in environment variable names.** `Greeting:Message=…` doesn't work in Bash, or in most container tooling. Fix: use `Greeting__Message`.
- **Wrong-case environment file on Linux.** `appsettings.production.json` is not loaded for `Production` on a case-sensitive file system, though it works on your Mac or Windows machine. Fix: match the environment name exactly: `appsettings.Production.json`.
- **Expecting `Development` on a server.** The environment defaults to `Production`. `launchSettings.json` only applies to `dotnet run` and the IDE. Fix: set `ASPNETCORE_ENVIRONMENT` explicitly wherever you need something other than `Production`.
- **Silent defaults from a typo.** `GetSection("Greting")` binds nothing, and every property keeps its default. Fix: a `SectionName` constant, plus `ValidateOnStart()` with `[Required]` on what must be set.
- **Secrets in `appsettings.json`.** They end up in source control and in every build output. Fix: user secrets locally, environment variables or a secret store when deployed.
- **`IOptionsSnapshot<T>` in a singleton.** A scoped service can't be captured by a singleton, and in `Development` the app fails at startup. Fix: use `IOptionsMonitor<T>` in singletons.
- **Expecting environment variable changes to apply live.** Environment variables are read once. Only reloadable providers, such as the JSON files, change while the app runs. Fix: restart, or redeploy, to pick up new environment values.

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
