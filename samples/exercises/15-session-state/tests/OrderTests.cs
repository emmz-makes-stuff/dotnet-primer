using System.Net;
using System.Net.Http.Json;

namespace SessionShop.Tests;

/// <summary>Shared tests for exercise 15's checkout.</summary>
public sealed class OrderTests
{
    [Fact]
    public async Task EmptyBasketCantBeCheckedOut()
    {
        using var shop = new ShopCluster();
        var shopper = new Shopper();

        using var response = await shopper.SendAsync(shop.A, HttpMethod.Post, "/checkout");

        Expect.Status(response, HttpStatusCode.BadRequest, "checking out an empty basket should be refused. " + Expect.Page);
    }

    [Fact]
    public async Task OrderOutlivesTheSession()
    {
        using var shop = new ShopCluster();
        var shopper = new Shopper();
        await shopper.AddAsync(shop.A, "SKU-1", 2);
        await shopper.AddAsync(shop.A, "SKU-3", 1);

        var placed = await shopper.CheckoutAsync(shop.A);
        Assert.Equal(9.99m, placed.Total);

        // A new shopper has no cookies: the order has to be somewhere other than their session.
        using var fetched = await new Shopper().SendAsync(shop.A, HttpMethod.Get, $"/orders/{placed.OrderId}");
        Expect.Status(fetched, HttpStatusCode.OK,
            "the order was found only with the session that placed it. Once that session expires, or the shopper comes back "
            + "on another device, the order is gone. Save it to OrderStore at checkout, and find it there (TODO 4). " + Expect.Page);

        var order = (await fetched.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken))!;
        Assert.True(
            order.Lines.SequenceEqual([new BasketLine("SKU-1", 2), new BasketLine("SKU-3", 1)]) && order.Total == 9.99m,
            $"the stored order has {string.Join(", ", order.Lines.Select(line => $"{line.Quantity} x {line.Sku}"))} for "
            + $"{order.Total}, expected 2 x SKU-1, 1 x SKU-3 for 9.99 (TODO 4). " + Expect.Page);
    }

    [Fact]
    public async Task CheckoutEmptiesTheBasket()
    {
        using var shop = new ShopCluster();
        var shopper = new Shopper();
        await shopper.AddAsync(shop.A, "SKU-2", 1);

        await shopper.CheckoutAsync(shop.A);
        var basket = await shopper.BasketAsync(shop.A);

        Assert.True(
            basket.Lines.Count == 0,
            $"after checkout, the basket still held {Expect.Lines(basket)}, so the shopper could order it twice. Empty the "
            + "basket in session once the order is placed (TODO 4). " + Expect.Page);
    }
}
