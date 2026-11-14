using System.Net;
using System.Net.Http.Json;

namespace SessionShop.Tests;

/// <summary>
/// One shopper's browser: it keeps the cookies the shop sets and sends them back, to whichever copy
/// of the shop it's talking to, the way a load balancer passes requests on.
/// </summary>
public sealed class Shopper
{
    private readonly Dictionary<string, string> _cookies = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> CookieNames => _cookies.Keys;

    public async Task AddAsync(ShopInstance shop, string sku, int quantity)
    {
        using var response = await SendAsync(shop, HttpMethod.Post, "/basket/items", JsonContent.Create(new AddItemRequest(sku, quantity)));
        Expect.Status(response, HttpStatusCode.OK, $"adding {quantity} x {sku} to the basket failed.");
    }

    public async Task<Basket> BasketAsync(ShopInstance shop)
    {
        using var response = await SendAsync(shop, HttpMethod.Get, "/basket");
        Expect.Status(response, HttpStatusCode.OK, "reading the basket failed.");
        return (await response.Content.ReadFromJsonAsync<Basket>(TestContext.Current.CancellationToken))!;
    }

    public async Task<CheckoutResult> CheckoutAsync(ShopInstance shop)
    {
        using var response = await SendAsync(shop, HttpMethod.Post, "/checkout");
        Expect.Status(response, HttpStatusCode.Created, "checkout failed.");
        return (await response.Content.ReadFromJsonAsync<CheckoutResult>(TestContext.Current.CancellationToken))!;
    }

    public async Task<HttpResponseMessage> SendAsync(ShopInstance shop, HttpMethod method, string path, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        if (_cookies.Count > 0)
        {
            request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
        }

        var response = await shop.Client.SendAsync(request, TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var setCookie in setCookies)
            {
                var pair = setCookie.Split(';', 2)[0];
                var equals = pair.IndexOf('=', StringComparison.Ordinal);
                _cookies[pair[..equals]] = pair[(equals + 1)..];
            }
        }

        return response;
    }
}
