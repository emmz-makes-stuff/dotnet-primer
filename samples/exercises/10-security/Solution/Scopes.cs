using System.Security.Claims;

namespace SecureOrders;

public static class Scopes
{
    public const string WriteOrders = "orders:write";

    /// <summary>
    /// Whether the user's token grants <paramref name="scope"/>. An identity provider sends the
    /// scopes as one space-separated string (<c>"orders:read orders:write"</c>), and
    /// <c>dotnet user-jwts</c> as an array, which becomes one <c>scope</c> claim per value. So check
    /// every <c>scope</c> claim, and split each one.
    /// </summary>
    public static bool HasScope(this ClaimsPrincipal user, string scope) =>
        user.FindAll("scope").Any(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(scope, StringComparer.Ordinal));
}
