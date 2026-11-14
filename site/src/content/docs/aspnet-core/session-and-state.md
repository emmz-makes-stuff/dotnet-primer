---
title: "Session and state"
description: "ASP.NET Core session: turning it on, typed access through JSON, why it has no per-user lock, running it on more than one server with a distributed cache and a shared Data Protection key ring, and what belongs somewhere other than session."
learn:
  - title: "Session in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/fundamentals/app-state
  - title: "Distributed caching in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/performance/caching/distributed
  - title: "Configure ASP.NET Core Data Protection"
    url: https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview
  - title: "ASP.NET to ASP.NET Core session state migration"
    url: https://learn.microsoft.com/aspnet/core/migration/fx-to-core/areas/session
---

By the end of this page you'll be able to turn on session in an ASP.NET Core app, store a typed object in it, explain what happens when two requests change one session at once, run session on more than one server, and decide what belongs in session and what doesn't.

:::tip[Cheatsheet]
Keep the [Session and state cheatsheet](/cheatsheets/session-and-state/) open while you work.
:::

## Turning on session

Session is middleware, and it keeps its data in an `IDistributedCache`. So it needs three things: a cache, the session services, and the middleware.

```csharp title="Program.cs"
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseSession();
```

`UseSession` goes before the endpoints that use session, and after `UseRouting` if the app calls it. Without a cache, the app fails at startup with "Unable to resolve service for type `IDistributedCache`".

How it works:

- The browser gets a cookie, `.AspNetCore.Session`, that holds the session's id, encrypted and signed by [Data Protection](/security/#data-protection). The data itself stays on the server, in the cache.
- A session that's never written to isn't kept. It gets a cookie only once a value is set, so until then every request starts a new one.
- A session expires 20 minutes after its last request, by default (`IdleTimeout`). Nothing tells the app when it does.

`AddDistributedMemoryCache` keeps the cache in the app's own memory. That's fine on one server, and nowhere else: see [More than one server](#more-than-one-server).

## Reading and writing session

`ISession` stores bytes. Its extension methods add strings and integers: `GetString`, `SetString`, `GetInt32` and `SetInt32`. There's no `Session["basket"] = basket`, because every value has to survive a trip to the cache, even the in-memory one. So an object goes in as JSON, through two extension methods of your own:

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

An endpoint reads the session from its `HttpContext`:

```csharp title="Program.cs"
app.MapPost("/basket/items", async (AddItemRequest request, HttpContext context) =>
{
    if (request.Sku is null || !PriceList.Sells(request.Sku) || request.Quantity < 1)
    {
        return Results.BadRequest("Send a SKU the shop sells, and a quantity of at least 1.");
    }

    var session = context.Session;
    await session.LoadAsync(context.RequestAborted);
    var basket = session.GetJson<Basket>(SessionKeys.Basket) ?? new Basket();
    basket.Add(request.Sku, request.Quantity);
    session.SetJson(SessionKeys.Basket, basket);

    // Commit before answering, so that a failed write is an error, not a basket that silently
    // didn't change.
    await session.CommitAsync(context.RequestAborted);
    return Results.Ok(basket);
});
```

Two calls there matter more than they look:

- **`LoadAsync`** reads the session from the cache without blocking. Without it, the first `Get` or `Set` loads it synchronously, which ties up a thread for the round trip to Redis.
- **`CommitAsync`** writes it back, and throws if the cache is down. Without it, the middleware writes the session after the response, and if that fails, it logs the error and carries on: the shopper was told the item went in, and it didn't.

What a value looks like in the cache: session writes its own small format, with each value's bytes as they are. After adding 2 x SKU-1, the entry ends with the key, `basket`, and then `{"Lines":[{"Sku":"SKU-1","Quantity":2}],"Total":9.00}`.

## Two requests at once

System.Web locked the session: a second request from the same session waited until the first had finished, unless the page or controller said it only read session. **ASP.NET Core takes no lock.** Two requests from one shopper run at the same time, each loads the session, and the one that commits last wins.

From a real run of the endpoint above, with a 200 ms pause added between loading the basket and writing it back. The basket held SKU-1, and two requests at once added SKU-2 and SKU-3:

```
{"lines":[{"sku":"SKU-1","quantity":1},{"sku":"SKU-2","quantity":1}],"total":16.50}
{"lines":[{"sku":"SKU-1","quantity":1},{"sku":"SKU-3","quantity":1}],"total":5.49}
```

Both answered as if their item went in. Reading the basket afterwards:

```
{"lines":[{"sku":"SKU-1","quantity":1},{"sku":"SKU-2","quantity":1}],"total":16.50}
```

SKU-3 is gone. A browser with two tabs, a double-clicked button, or a page that fires several requests at once does this without trying.

What to do about it:

- Keep in session only what one request at a time changes, such as a step in a form.
- Keep anything that requests change concurrently, such as a basket in a busy shop, in a database, with a [concurrency token](/data/ef-core/#concurrency) so that a second write fails instead of overwriting the first. Session can hold its id.
- Don't build a lock of your own around session. It would hold up every request from that shopper, on every server, which is the System.Web behaviour with none of its tooling.

## More than one server

The in-memory cache is per process. Put two copies of the app behind a load balancer, and the second request can land on the copy that has never seen the session: the shopper's basket is empty, with no error. Two things have to be shared for session to work across servers.

**1. The cache.** Register a distributed one, such as Redis, in place of `AddDistributedMemoryCache`. It needs the `Microsoft.Extensions.Caching.StackExchangeRedis` package:

```csharp title="Program.cs"
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "shop:";
});
```

On AWS, that's ElastiCache. `AddDistributedSqlServerCache` keeps it in SQL Server instead.

**2. The Data Protection key ring.** The session cookie is encrypted, so every server has to be able to decrypt the others' cookies. By default each one can't:

- **Copies of one container image** all run from the same folder, `/app`, but each container makes its own key ring in its own file system, and makes a new one every time it starts. A cookie from one container is unreadable in the next, and every deploy loses every session.
- **Apps on different servers or folders** are kept apart even when they share a key ring: Data Protection isolates apps by their content root path unless you give them one name.

A server that can't read the cookie doesn't fail. It ignores the cookie and starts a new, empty session. So the symptom is a basket that empties now and then, at random, which is hard to trace back to keys.

Give every copy the same key ring and the same application name:

```csharp title="Program.cs"
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeysPath"]!))
    .SetApplicationName("shop");
```

A shared folder works on one host, or on a file share. In containers, keep the ring in Redis, a database, or AWS Parameter Store: the [Security page's Data protection section](/security/#data-protection) has the providers.

**Sticky sessions**, where the load balancer sends a shopper back to the same server, hide the problem until that server restarts or is scaled in. Use them for performance if you like, not instead of a shared cache and key ring.

## What belongs in session

Session data expires, can be lost when the cache restarts, and is gone if the shopper switches device. So nothing the user can't recreate belongs only in session.

| Data                                                             | Where it goes                                                                                         |
| ---------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| A placed order, a saved address, anything the business must keep | A database. Session can keep its id.                                                                  |
| Who the user is, and their roles                                 | The authentication cookie or token's claims: see [Security & auth](/security/)                        |
| A small display preference, such as a currency                   | A cookie of its own, or the client                                                                    |
| Data the app looked up and could look up again                   | A cache: [HybridCache](/performance/async-and-performance/#hybridcache), shared by every user it fits |
| A wizard's step, a basket before checkout                        | Session                                                                                               |

The exercise moves a placed order out of session. At checkout, the order goes in the store, the basket is emptied, and session keeps only the order's id:

```csharp title="Program.cs"
var order = orders.Place(basket.Lines);
session.Remove(SessionKeys.Basket);
session.SetInt32(SessionKeys.LastOrderId, order.Id);
await session.CommitAsync(context.RequestAborted);
```

## Coming from .NET 3.5

| In System.Web                                                    | In ASP.NET Core                                                                                      |
| ---------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| `Session["basket"] = basket`                                     | [`session.SetJson("basket", basket)`](#reading-and-writing-session), an extension method of your own |
| `(Basket)Session["basket"]`                                      | [`session.GetJson<Basket>("basket")`](#reading-and-writing-session), `null` when it isn't there      |
| `HttpContext.Current.Session`                                    | `context.Session`, from the endpoint's `HttpContext`, or `IHttpContextAccessor` in a service         |
| `<sessionState mode="InProc">`                                   | [`AddDistributedMemoryCache`](#turning-on-session): one process only                                 |
| `mode="StateServer"` or `mode="SQLServer"`                       | A [shared `IDistributedCache`](#more-than-one-server): Redis, or `AddDistributedSqlServerCache`      |
| The session lock: one request at a time per session              | [No lock](#two-requests-at-once): concurrent requests can overwrite each other                       |
| `EnableSessionState="ReadOnly"`, `[SessionState(ReadOnly)]`      | Nothing to set, as there's no lock to avoid                                                          |
| `<machineKey>`, the same on every server in the farm             | [A shared Data Protection key ring and `SetApplicationName`](#more-than-one-server)                  |
| `Session_Start` in `Global.asax`                                 | No event: check whether the value you need is set yet                                                |
| `Session_End` in `Global.asax`                                   | No event, and it only ever fired for InProc: clean up expired data with a scheduled job              |
| `TempData` in MVC                                                | MVC's `TempData`, which keeps its data in a cookie by default                                        |
| `cookieless="true"`, the session id in the URL                   | Not supported: the session id is always in a cookie                                                  |
| An order or a user's details kept in session for the whole visit | [A database](#what-belongs-in-session), with at most an id in session                                |

## Pitfalls

- **The basket empties at random behind a load balancer.** The servers don't share the cache, or can't read each other's cookie, and each one quietly starts a new session. Fix: a [distributed cache and a shared key ring](#more-than-one-server), with one application name.
- **Every deploy signs everyone out of their session.** Each new container makes a new key ring, so old cookies are unreadable. Fix: keep the key ring outside the container.
- **No `CommitAsync`.** A failed write to the cache is logged, and the response says it worked. Fix: `await session.CommitAsync()` before answering, after the last change.
- **No `LoadAsync`.** The first read blocks a thread while the session loads. Fix: `await session.LoadAsync()` first.
- **A new session on every request.** Nothing was written to it, so it was never kept, and no cookie was sent. Fix: that's by design. Don't rely on the session's id until you've set a value.
- **The session cookie is never set.** A cookie-consent policy (`UseCookiePolicy` with `CheckConsentNeeded`) drops cookies that aren't essential, and the session cookie isn't by default. Fix: `options.Cookie.IsEssential = true`, if session is essential to the app.
- **Large objects in session.** Every request loads and writes the whole session, so a cached product list or an entity graph slows every page. Fix: keep ids and small values in session, and look the rest up.
- **"Unable to resolve service for type `IDistributedCache`".** `AddSession` was called without a cache. Fix: register one, such as `AddDistributedMemoryCache`.

## Exercise 15: move the basket into session

**Goal:** take a shop whose baskets live in the server's own memory, the way InProc session did, and make it work on two servers: the basket in ASP.NET Core session, read and written as JSON, both servers sharing a Data Protection key ring, and the placed order moved out of session altogether.

**Where:** [`samples/exercises/15-session-state`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/15-session-state) in the GitHub repo. Clone the repo, then work in the `Starter` project. **It needs no Docker and no Redis**: the tests run two copies of the shop inside the test process, sharing one in-memory distributed cache. They give each copy a folder of its own, as two servers have, so that Data Protection keeps them apart until TODO 3.

**Steps:**

1. In `Starter/Program.cs`, register a distributed cache, `AddSession` and `UseSession`, and remove `ServerBaskets` and its `basket-id` cookie (TODO 1).
2. Write `GetJson` and `SetJson` in `Starter/SessionExtensions.cs`, and use them for the basket (TODO 2).
3. Configure Data Protection from configuration: persist the keys to the folder in `DataProtection:KeysPath`, and set the name in `DataProtection:ApplicationName` (TODO 3).
4. At checkout, save the order to `OrderStore`, empty the basket, and keep only the order's id in session. Find orders in the store (TODO 4).

**Check your work:** the tests start red and turn green as you go. Each failure names the TODO that fixes it.

```sh
dotnet test --project samples/exercises/15-session-state/Starter.Tests
```

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
