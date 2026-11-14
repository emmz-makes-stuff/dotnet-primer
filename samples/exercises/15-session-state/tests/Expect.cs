using System.Net;

namespace SessionShop.Tests;

public static class Expect
{
    /// <summary>Where a failing test sends the reader.</summary>
    public const string Page = "See the Session and state page (/aspnet-core/session-and-state/).";

    public static void Status(HttpResponseMessage response, HttpStatusCode expected, string hint)
    {
        if (response.StatusCode != expected)
        {
            var request = response.RequestMessage!;
            Assert.Fail(
                $"{request.Method} {request.RequestUri!.PathAndQuery} returned {(int)response.StatusCode} {response.StatusCode}, "
                + $"expected {(int)expected} {expected}: {hint}{ExceptionIn(response)}");
        }
    }

    /// <summary>
    /// The exception behind a 500, from the developer exception page's plain-text body (the tests'
    /// host runs in Development), such as a TODO's NotImplementedException.
    /// </summary>
    private static string ExceptionIn(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.InternalServerError)
        {
            return "";
        }

        var body = response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();
        var first = body.Split('\n', 2)[0].Trim();
        return first.Length == 0 ? "" : $" The app threw {first}";
    }

    public static string Lines(Basket basket) =>
        basket.Lines.Count == 0 ? "an empty basket" : string.Join(", ", basket.Lines.Select(line => $"{line.Quantity} x {line.Sku}"));
}
