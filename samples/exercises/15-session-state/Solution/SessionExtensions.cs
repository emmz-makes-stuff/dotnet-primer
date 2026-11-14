using System.Text.Json;

namespace SessionShop;

/// <summary>The keys the shop uses in session.</summary>
public static class SessionKeys
{
    public const string Basket = "basket";
    public const string LastOrderId = "last-order-id";
}

/// <summary>
/// Typed access to session. <see cref="ISession"/> stores only bytes (and, through its own
/// extension methods, strings and integers), so an object goes in as JSON.
/// </summary>
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
