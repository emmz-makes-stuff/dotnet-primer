---
title: Configuration cheatsheet
description: Configuration providers, key naming, user secrets and the options pattern, on one page.
prev: false
next: false
learn:
  - title: Configuration in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/
  - title: Options pattern in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options
  - title: Safe storage of app secrets in development
    url: https://learn.microsoft.com/aspnet/core/security/app-secrets
---

Back to [Configuration](/aspnet-core/configuration/).

## Who wins

Default sources for `WebApplication.CreateBuilder`, **highest priority first**:

| #   | Source                                                       | Example                              |
| --- | ------------------------------------------------------------ | ------------------------------------ |
| 1   | Command-line arguments                                       | `--Greeting:Message=Hi`              |
| 2   | Environment variables (not `ASPNETCORE_`/`DOTNET_`-prefixed) | `Greeting__Message=Hi`               |
| 3   | User secrets (`Development` only)                            | `dotnet user-secrets set …`          |
| 4   | `appsettings.{Environment}.json`                             | `appsettings.Production.json`        |
| 5   | `appsettings.json`                                           |                                      |
| 6   | Host configuration                                           | `ASPNETCORE_ENVIRONMENT`, `DOTNET_…` |

## Key naming

| Where                | Hierarchy separator      | `Smtp` → `Host`    | Array item 0                |
| -------------------- | ------------------------ | ------------------ | --------------------------- |
| Code, JSON path      | `:`                      | `Smtp:Host`        | `Smtp:Recipients:0`         |
| Environment variable | `__` (double underscore) | `Smtp__Host`       | `Smtp__Recipients__0`       |
| Command line         | `:`                      | `--Smtp:Host=mail` | `--Smtp:Recipients:0=a@b.c` |

Keys are **case-insensitive**. Connection strings live under `ConnectionStrings`:
`ConnectionStrings__Orders` → `builder.Configuration.GetConnectionString("Orders")`.

## Environments

```sh
ASPNETCORE_ENVIRONMENT=Staging dotnet run   # selects appsettings.Staging.json
dotnet run --environment Staging            # same, from the command line
```

The default is `Production` when nothing sets it. `dotnet run` uses
`Properties/launchSettings.json`, which sets `Development`. On Linux, file names are case-sensitive:
`appsettings.development.json` is **not** loaded for `Development`.

## User secrets (local development only)

```sh
dotnet user-secrets init                               # adds UserSecretsId to the .csproj
dotnet user-secrets set "Smtp:Password" "p@ssw0rd"
dotnet user-secrets list
dotnet user-secrets remove "Smtp:Password"
dotnet user-secrets clear
```

Stored unencrypted in your user profile, outside the repo. Use a real secret store (AWS Secrets
Manager, Parameter Store) in deployed environments.

## Reading values

```csharp title="Program.cs"
string? message = builder.Configuration["Greeting:Message"];
int pageSize = builder.Configuration.GetValue("Paging:PageSize", defaultValue: 20);
string? orders = builder.Configuration.GetConnectionString("Orders");
IConfigurationSection smtp = builder.Configuration.GetSection("Smtp");
bool configured = smtp.Exists();
```

## Options: register

```csharp title="Program.cs"
// Bind a section
builder.Services.Configure<GreetingOptions>(builder.Configuration.GetSection("Greeting"));

// Bind, validate, and fail at startup if invalid
builder.Services.AddOptions<SmtpOptions>()
    .BindConfiguration("Smtp")
    .ValidateDataAnnotations()                       // [Required], [Range], …
    .Validate(o => o.Port is 25 or 465 or 587, "Smtp:Port must be 25, 465 or 587.")
    .ValidateOnStart();

// Named options: several instances of one type
builder.Services.Configure<StorageOptions>("images", builder.Configuration.GetSection("Storage:Images"));
builder.Services.Configure<StorageOptions>("logs", builder.Configuration.GetSection("Storage:Logs"));
```

Keep options classes simple: a public parameterless constructor and public `{ get; set; }` properties
bind reliably, with defaults as property initializers.

## Options: consume

| Inject                                                               | Lifetime  | Sees changes?               | Use for                                  |
| -------------------------------------------------------------------- | --------- | --------------------------- | ---------------------------------------- |
| `IOptions<T>` → `.Value`                                             | singleton | no, read once               | settings that never change at runtime    |
| `IOptionsSnapshot<T>` → `.Value`, `.Get(name)`                       | scoped    | yes, recomputed per request | per-request reads; **not** in singletons |
| `IOptionsMonitor<T>` → `.CurrentValue`, `.Get(name)`, `.OnChange(…)` | singleton | yes, live                   | singletons and background services       |

`appsettings*.json` reload on change by default. Environment variables and the command line are read
once at startup.

## Test overrides

```csharp title="Tests.cs"
factory.WithWebHostBuilder(host => host.ConfigureAppConfiguration(config =>
    config.AddInMemoryCollection(new Dictionary<string, string?> { ["Greeting:Message"] = "test" })));
```
