using System.Net;
using System.Net.Http.Json;

namespace SecureOrders.Tests;

/// <summary>Is the caller allowed? A valid token is only the start: each endpoint has its own rule.</summary>
public sealed class AuthorizationTests(SecureOrdersApp app) : IClassFixture<SecureOrdersApp>
{
    private static readonly PlaceOrderRequest _order = new([new("SKU-1", 2)]);

    [Fact]
    public async Task NewEndpointIsPrivateByDefault()
    {
        // GET /catalogue has no authorization of its own, like any endpoint someone adds later.
        using var client = app.Client();

        using var response = await client.GetAsync("/catalogue", TestContext.Current.CancellationToken);

        Expect.Status(response, HttpStatusCode.Unauthorized,
            "an endpoint with no authorization of its own answered a caller with no token. "
            + "Set a fallback policy that requires an authenticated user (TODO 2).");
    }

    [Fact]
    public async Task PlacingAnOrderNeedsTheWriteScope()
    {
        var ct = TestContext.Current.CancellationToken;
        using var withoutScope = app.Client(app.Token("cust-1"));
        using var withScope = app.Client(app.Token("cust-1", scope: "orders:write"));

        using var refused = await withoutScope.PostAsJsonAsync("/orders", _order, ct);
        using var placed = await withScope.PostAsJsonAsync("/orders", _order, ct);

        Expect.Status(refused, HttpStatusCode.Forbidden,
            "a token without the orders:write scope placed an order. Require the PlaceOrders policy on POST /orders (TODO 3).");
        Expect.Status(placed, HttpStatusCode.Created,
            "a token with the orders:write scope couldn't place an order (TODO 3).");
    }

    [Fact]
    public async Task ScopesSentAsOneStringAreSplit()
    {
        // Identity providers send every granted scope in one space-separated `scope` claim.
        using var client = app.Client(app.Token("cust-1", scope: "orders:read orders:write"));

        using var response = await client.PostAsJsonAsync("/orders", _order, TestContext.Current.CancellationToken);

        Expect.Status(response, HttpStatusCode.Created,
            "a token with scope \"orders:read orders:write\" was refused. The scope claim is one space-separated string, "
            + "so RequireClaim(\"scope\", \"orders:write\") doesn't match it: split it, in the PlaceOrders policy (TODO 3).");
    }

    [Fact]
    public async Task CustomerReadsTheirOwnOrderButNotAnothers()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = await app.PlaceOrderAsync("cust-1", ct);
        using var owner = app.Client(app.Token("cust-1"));
        using var someoneElse = app.Client(app.Token("cust-2"));

        using var own = await owner.GetAsync($"/orders/{order.Id}", ct);
        using var another = await someoneElse.GetAsync($"/orders/{order.Id}", ct);

        Assert.True(order.CustomerId == "cust-1",
            $"the order was stored for customer \"{order.CustomerId}\", not the token's sub \"cust-1\": the app didn't read "
            + "the token's sub claim. Add JWT bearer authentication, with inbound claim mapping off so sub keeps its name (TODO 1).");
        Expect.Status(own, HttpStatusCode.OK, "a customer couldn't read their own order (TODO 4).");
        Expect.Status(another, HttpStatusCode.Forbidden,
            "customer cust-2 read cust-1's order. Load the order, then check the ReadOrder policy against it "
            + "with IAuthorizationService (TODO 4).");
    }

    [Fact]
    public async Task SupportReadsAndRefundsAnyOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = await app.PlaceOrderAsync("cust-1", ct);
        using var customer = app.Client(app.Token("cust-1"));
        using var support = app.Client(app.Token("staff-1", roles: ["support"]));

        using var customerRefund = await customer.PostAsync($"/orders/{order.Id}/refund", null, ct);
        using var read = await support.GetAsync($"/orders/{order.Id}", ct);
        using var refund = await support.PostAsync($"/orders/{order.Id}/refund", null, ct);

        Expect.Status(customerRefund, HttpStatusCode.Forbidden,
            "a customer refunded their own order. Require the Refunds policy, for the support role, on the refund endpoint (TODO 5).");
        Expect.Status(read, HttpStatusCode.OK,
            "support staff couldn't read a customer's order. The ReadOrder handler allows the support role too (TODO 4), "
            + "and the role claim must be the token's role claim (TODO 1).");
        Expect.Status(refund, HttpStatusCode.NoContent, "support staff couldn't refund an order (TODO 5).");
    }

    [Fact]
    public async Task UnknownOrderIsNotFound()
    {
        using var client = app.Client(app.Token("staff-1", roles: ["support"]));

        using var response = await client.GetAsync("/orders/999999", TestContext.Current.CancellationToken);

        Expect.Status(response, HttpStatusCode.NotFound, "an order id that doesn't exist must answer 404.");
    }
}
