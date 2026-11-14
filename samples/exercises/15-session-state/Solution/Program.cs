using Microsoft.AspNetCore.DataProtection;
using SessionShop;

var builder = WebApplication.CreateBuilder(args);

// TODO 1 (solved): session, backed by IDistributedCache. The in-memory cache is per process; with
//   more than one instance, register a shared one instead, such as AddStackExchangeRedisCache.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// TODO 3 (solved): every instance must read the others' session cookies, so they share one key
//   ring and one application name. Without a name, Data Protection keeps apps apart by their
//   content root path; without a shared key ring, each instance makes keys of its own.
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName(builder.Configuration["DataProtection:ApplicationName"] ?? "session-shop");
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

builder.Services.AddSingleton<OrderStore>();

var app = builder.Build();

app.UseSession();

app.MapGet("/basket", async (HttpContext context) =>
{
    await context.Session.LoadAsync(context.RequestAborted);

    // TODO 2 (solved): the basket is read and written as JSON.
    return context.Session.GetJson<Basket>(SessionKeys.Basket) ?? new Basket();
});

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

app.MapPost("/checkout", async (HttpContext context, OrderStore orders) =>
{
    var session = context.Session;
    await session.LoadAsync(context.RequestAborted);
    var basket = session.GetJson<Basket>(SessionKeys.Basket);
    if (basket is null || basket.Lines.Count == 0)
    {
        return Results.BadRequest("The basket is empty.");
    }

    // TODO 4 (solved): the order is the shop's to keep, so it goes in the store. Session keeps
    //   only its id, and the basket is emptied.
    var order = orders.Place(basket.Lines);
    session.Remove(SessionKeys.Basket);
    session.SetInt32(SessionKeys.LastOrderId, order.Id);
    await session.CommitAsync(context.RequestAborted);

    return Results.Created($"/orders/{order.Id}", new CheckoutResult(order.Id, order.Total));
});

app.MapGet("/orders/{id:int}", (int id, OrderStore orders) =>
    orders.Find(id) is { } order ? Results.Ok(order) : Results.NotFound());

app.Run();

/// <summary>Visible to the tests' <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
