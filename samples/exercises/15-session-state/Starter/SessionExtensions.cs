namespace SessionShop;

/// <summary>The keys the shop uses in session.</summary>
public static class SessionKeys
{
    public const string Basket = "basket";
    public const string LastOrderId = "last-order-id";
}

/// <summary>
/// Typed access to session.
/// </summary>
/// <remarks>
/// TODO 2: ISession stores only bytes, and its own extension methods add strings and integers
///   (GetString, SetString, GetInt32, SetInt32). There's no Session["basket"] = basket. Write these
///   two so that an object goes in as JSON and comes back as the same type, then use them for the
///   basket in Program.cs.
/// </remarks>
public static class SessionExtensions
{
    public static T? GetJson<T>(this ISession session, string key) =>
        throw new NotImplementedException("TODO 2: read the value as a string, and deserialize it from JSON.");

    public static void SetJson<T>(this ISession session, string key, T value) =>
        throw new NotImplementedException("TODO 2: serialize the value to JSON, and store it as a string.");
}
