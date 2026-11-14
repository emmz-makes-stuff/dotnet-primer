using SessionShop;

var builder = WebApplication.CreateBuilder(args);

// TODO 1: Each shopper's basket lives in this server's memory (ServerBaskets.cs), the way InProc
//   session did. Behind a load balancer, the next request can land on a server that has never seen
//   it. Keep the basket in ASP.NET Core session instead: register an IDistributedCache
//   (AddDistributedMemoryCache will do here), AddSession, and UseSession below. Then remove
//   ServerBaskets and its basket-id cookie: session has a cookie of its own.
builder.Services.AddSingleton<ServerBaskets>();

// TODO 3: The tests run two copies of the shop, as two servers would, sharing one distributed
//   cache. Each copy can't read the other's session cookie, because Data Protection, which signs
//   and encrypts it, keeps apps apart unless they share a key ring and an application name. Call
//   AddDataProtection, persist the keys to the folder in DataProtection:KeysPath, and set the
//   application name from DataProtection:ApplicationName.

// TODO 4: Orders belong in OrderStore (Shop.cs), which stands in for the shop's database.
builder.Services.AddSingleton<OrderStore>();

var app = builder.Build();

app.MapGet("/basket", (HttpContext context, ServerBaskets baskets) => baskets.For(context).Basket);

app.MapPost("/basket/items", (AddItemRequest request, HttpContext context, ServerBaskets baskets) =>
{
    if (request.Sku is null || !PriceList.Sells(request.Sku) || request.Quantity < 1)
    {
        return Results.BadRequest("Send a SKU the shop sells, and a quantity of at least 1.");
    }

    // TODO 2: Load the session, read the basket with GetJson, add the item, write it back with
    //   SetJson, and commit before answering.
    var basket = baskets.For(context).Basket;
    basket.Add(request.Sku, request.Quantity);
    return Results.Ok(basket);
});

app.MapPost("/checkout", (HttpContext context, ServerBaskets baskets) =>
{
    var shopper = baskets.For(context);
    if (shopper.Basket.Lines.Count == 0)
    {
        return Results.BadRequest("The basket is empty.");
    }

    // TODO 4: The order is kept only with the shopper, as the last thing in their session was in
    //   the old shop. Once the session expires, or the shopper comes back on another device, it's
    //   gone. Place it in OrderStore, keep at most its id in session (SetInt32 with
    //   SessionKeys.LastOrderId), and empty the basket.
    var order = baskets.PlaceOrder(shopper);
    return Results.Created($"/orders/{order.Id}", new CheckoutResult(order.Id, order.Total));
});

// TODO 4: And find the order in OrderStore here.
app.MapGet("/orders/{id:int}", (int id, HttpContext context, ServerBaskets baskets) =>
    baskets.For(context).LastOrder is { } order && order.Id == id ? Results.Ok(order) : Results.NotFound());

app.Run();

/// <summary>Visible to the tests' <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
