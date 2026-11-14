using System.Collections.Concurrent;

namespace SessionShop;

/// <summary>What the shop remembers about one shopper.</summary>
public sealed class ShopperState
{
    public Basket Basket { get; } = new();

    public Order? LastOrder { get; set; }
}

/// <summary>
/// Each shopper's basket, in this server's memory, found by a <c>basket-id</c> cookie. It's how
/// InProc session worked in System.Web, and it has the same problem: a second server has a
/// dictionary of its own, and a restart empties it.
/// </summary>
/// <remarks>
/// TODO 1: Delete this file once the basket is in session. A singleton, not a static, so that two
///   copies of the app in one test process don't share it by accident: two servers wouldn't.
/// </remarks>
public sealed class ServerBaskets
{
    private const string _cookieName = "basket-id";

    private readonly ConcurrentDictionary<string, ShopperState> _shoppers = new();
    private int _lastOrderId;

    public ShopperState For(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(_cookieName, out var id))
        {
            id = Guid.NewGuid().ToString("N");
            context.Response.Cookies.Append(_cookieName, id, new CookieOptions { HttpOnly = true, IsEssential = true });
        }

        return _shoppers.GetOrAdd(id, _ => new ShopperState());
    }

    public Order PlaceOrder(ShopperState shopper)
    {
        var lines = shopper.Basket.Lines;
        var order = new Order(Interlocked.Increment(ref _lastOrderId), [.. lines], PriceList.Total(lines));
        shopper.LastOrder = order;
        return order;
    }
}
