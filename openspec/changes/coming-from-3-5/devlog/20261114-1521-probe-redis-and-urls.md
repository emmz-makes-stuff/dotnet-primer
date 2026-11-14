# Probe: Redis example and Learn URLs (1.2)

## Redis example compiles

In the scratch tree (see `20261114-1521-probe-two-hosts.md`), a `Redis` web project referencing
`Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.14 (scratch `Directory.Packages.props` only;
the repo's doesn't change) built with `0 Warning(s)`, `0 Error(s)`, warnings as errors, with:

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "shop:";
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeysPath"]!))
    .SetApplicationName("shop");

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

The lost-write run for the page is recorded in the 1.1 note.

## Learn URLs the page will cite (all 200 on 2026-11-14)

- https://learn.microsoft.com/aspnet/core/fundamentals/app-state ("Session in ASP.NET Core")
- https://learn.microsoft.com/aspnet/core/performance/caching/distributed
- https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview
- https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/default-settings
- https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers
- https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.http.isession
- https://learn.microsoft.com/aspnet/core/migration/fx-to-core/areas/session (Coming from table
  only; it's porting guidance, so cite it as further reading, not as a recipe)

## Facts from Learn for the page

- "All session data must be serialized to enable a distributed cache scenario, even when using the
  in-memory cache." String and integer helpers only; complex types are the app's job (JSON).
- `LoadAsync` before reading, or the session loads synchronously. `CommitAsync` throws if the store
  is down. Without it, the middleware logs the failure and the request carries on as if it worked
  (a Pitfall).
- Empty sessions aren't kept: a session needs one value set before its cookie persists. The default
  idle timeout is 20 minutes. Nothing tells the app when a session ends (so no `Session_End`).
- Data Protection: "By default, the Data Protection system isolates apps from one another based on
  their content root paths, even if they share the same physical key repository" (confirms 1.1).
  And "in a docker container the app always has the same path and, therefore, the same application
  discriminator". So **in containers the problem is the key ring, not the name**: each replica makes
  its own keys in its own filesystem, and loses them on restart. The page must say both. The
  exercise's distinct content roots stand in for two servers; the page explains the container case.

## Next

§1 is done once the gates are green. Then §2, task 2.1: scaffold
`samples/exercises/15-session-state/`.
