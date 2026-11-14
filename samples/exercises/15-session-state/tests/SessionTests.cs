using System.Text.RegularExpressions;

namespace SessionShop.Tests;

/// <summary>
/// Shared tests for exercise 15's basket. Compiled into both Starter.Tests and Solution.Tests, so
/// they run against whichever app the project references.
/// </summary>
public sealed partial class SessionTests
{
    private const string _sessionCookie = ".AspNetCore.Session";

    [Fact]
    public async Task BasketKeepsItsItems()
    {
        using var shop = new ShopCluster();
        var shopper = new Shopper();

        await shopper.AddAsync(shop.A, "SKU-1", 2);
        await shopper.AddAsync(shop.A, "SKU-2", 1);
        await shopper.AddAsync(shop.A, "SKU-1", 1);
        var basket = await shopper.BasketAsync(shop.A);

        Assert.True(
            basket.Lines.SequenceEqual([new BasketLine("SKU-1", 3), new BasketLine("SKU-2", 1)]),
            $"after adding 2 x SKU-1, 1 x SKU-2 and 1 x SKU-1, the basket held {Expect.Lines(basket)}, expected 3 x SKU-1, 1 x SKU-2. "
            + Expect.Page);
    }

    [Fact]
    public async Task BasketIsKeptInSession()
    {
        using var shop = new ShopCluster();
        var shopper = new Shopper();

        await shopper.AddAsync(shop.A, "SKU-1", 1);

        Assert.True(
            shopper.CookieNames.Contains(_sessionCookie),
            $"adding to the basket set the cookies [{string.Join(", ", shopper.CookieNames)}], with no {_sessionCookie} cookie, so "
            + "the basket isn't in ASP.NET Core session. Register a distributed cache, AddSession and UseSession, and keep the "
            + "basket there (TODO 1). " + Expect.Page);
        Assert.False(
            shopper.CookieNames.Contains("basket-id"),
            "the shop still sets the basket-id cookie, so the basket is still in ServerBaskets, this server's own memory. Remove "
            + "ServerBaskets once the basket is in session (TODO 1). " + Expect.Page);
    }

    [Fact]
    public async Task BasketIsStoredInSessionAsJson()
    {
        using var shop = new ShopCluster();
        var shopper = new Shopper();

        await shopper.AddAsync(shop.A, "SKU-2", 5);

        var writes = shop.Cache.Writes;
        Assert.True(
            writes.Count > 0,
            "nothing was written to the distributed cache, so session isn't in use yet (TODO 1). " + Expect.Page);
        Assert.True(
            writes.Any(write => BasketJson().IsMatch(write)),
            "session was written, but no value in it holds the basket as JSON, with \"sku\": \"SKU-2\" and \"quantity\": 5. "
            + "Write GetJson and SetJson in SessionExtensions.cs, and use them for the basket (TODO 2). " + Expect.Page);
    }

    [Fact]
    public async Task BasketFollowsTheShopperToTheOtherServer()
    {
        using var shop = new ShopCluster();
        var shopper = new Shopper();

        await shopper.AddAsync(shop.A, "SKU-3", 4);
        var onB = await shopper.BasketAsync(shop.B);

        Assert.True(
            onB.Lines.SequenceEqual([new BasketLine("SKU-3", 4)]),
            $"the shopper added 4 x SKU-3 on server A, and server B, with the same cookies, returned {Expect.Lines(onB)}. "
            + "If BasketIsKeptInSession fails too, start with TODO 1. Otherwise the servers share the cache but not Data "
            + "Protection, so B can't read A's session cookie, and quietly starts a new, empty session. Give both the key ring "
            + "in DataProtection:KeysPath and the name in DataProtection:ApplicationName (TODO 3). " + Expect.Page);
        Assert.True(
            Directory.EnumerateFiles(shop.KeysPath, "*.xml").Any(),
            "the servers read each other's cookie, but there's no key in the folder DataProtection:KeysPath names, so they're "
            + "sharing keys some other way, such as your user profile, which two real servers wouldn't. Persist the keys to "
            + "that folder (TODO 3). " + Expect.Page);
    }

    // "sku": "SKU-2" then "quantity": 5, in either case, as JsonSerializer writes them by default or with web options.
    [GeneratedRegex("""
        "sku"\s*:\s*"SKU-2"\s*,\s*"quantity"\s*:\s*5\b
        """, RegexOptions.IgnoreCase)]
    private static partial Regex BasketJson();
}
