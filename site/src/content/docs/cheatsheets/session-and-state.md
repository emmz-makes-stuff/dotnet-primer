---
title: Session and state cheatsheet
description: Turning session on, typed access through JSON, LoadAsync and CommitAsync, more than one server, what belongs elsewhere, and the System.Web mappings, on one page.
prev: false
next: false
learn:
  - title: Session in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/fundamentals/app-state
  - title: Distributed caching in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/performance/caching/distributed
  - title: Configure ASP.NET Core Data Protection
    url: https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview
---

Back to [Session and state](/aspnet-core/session-and-state/).

## Turning it on

```csharp title="Program.cs"
builder.Services.AddDistributedMemoryCache();   // one server; see "More than one server"
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;          // kept even under a cookie-consent policy
});

var app = builder.Build();
app.UseSession();                               // before the endpoints that use it
```

## Reading and writing

```csharp
var session = context.Session;
await session.LoadAsync(context.RequestAborted);       // load without blocking

var basket = session.GetJson<Basket>("basket") ?? new Basket();
session.SetJson("basket", basket);                      // your own extension methods, below
session.SetInt32("last-order-id", order.Id);
session.Remove("basket");

await session.CommitAsync(context.RequestAborted);     // throws if the cache is down
```

```csharp title="SessionExtensions.cs"
public static class SessionExtensions
{
    public static T? GetJson<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public static void SetJson<T>(this ISession session, string key, T value) =>
        session.SetString(key, JsonSerializer.Serialize(value));
}
```

| Behaviour            | What to know                                                                         |
| -------------------- | ------------------------------------------------------------------------------------ |
| What it stores       | Bytes, strings and integers. Objects go in as JSON.                                  |
| The cookie           | `.AspNetCore.Session`: the session's id, encrypted by Data Protection                |
| An unwritten session | Isn't kept, and gets no cookie                                                       |
| Expiry               | 20 minutes after the last request (`IdleTimeout`). No event when it happens.         |
| Locking              | None. Two requests at once each load the session, and the last to commit wins.       |
| A failed write       | Logged, and the response still succeeds, unless you call `CommitAsync` and handle it |

## More than one server

```csharp title="Program.cs"
// Package: Microsoft.Extensions.Caching.StackExchangeRedis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "shop:";
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeysPath"]!))
    .SetApplicationName("shop");
```

| Shared by every server | Why                                                                                          |
| ---------------------- | -------------------------------------------------------------------------------------------- |
| The cache              | The in-memory cache is per process                                                           |
| The key ring           | Each container makes its own, so one can't read another's cookie, or its own after a restart |
| The application name   | Without one, apps in different folders are kept apart even with one key ring                 |

A server that can't read the cookie starts a new, empty session, with no error.

## What goes where

| Data                                            | Where                                      |
| ----------------------------------------------- | ------------------------------------------ |
| A placed order, anything the business must keep | A database, with at most its id in session |
| Who the user is, their roles                    | Authentication claims                      |
| A small display preference                      | A cookie of its own                        |
| Data you could look up again                    | A cache, such as `HybridCache`             |
| A basket before checkout, a wizard's step       | Session                                    |

## Coming from System.Web

| System.Web                                        | ASP.NET Core                                                 |
| ------------------------------------------------- | ------------------------------------------------------------ |
| `Session["basket"] = basket`                      | `session.SetJson("basket", basket)`                          |
| `HttpContext.Current.Session`                     | `context.Session`, or `IHttpContextAccessor`                 |
| `mode="InProc"`                                   | `AddDistributedMemoryCache`                                  |
| `mode="StateServer"`, `mode="SQLServer"`          | `AddStackExchangeRedisCache`, `AddDistributedSqlServerCache` |
| The session lock, `EnableSessionState="ReadOnly"` | No lock                                                      |
| `<machineKey>` on every server                    | A shared Data Protection key ring and name                   |
| `Session_Start`, `Session_End`                    | No events                                                    |
| `cookieless="true"`                               | Not supported                                                |
