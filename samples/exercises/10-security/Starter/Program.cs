using System.Security.Claims;
using SecureOrders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OrderStore>();

// This API works, but anyone can do anything: there's no authentication or authorization yet.
// Run the tests to see what's missing: dotnet test --project samples/exercises/10-security/Starter.Tests

// TODO 1: Add JWT bearer authentication: builder.Services.AddAuthentication().AddJwtBearer(...).
//   Leave the issuer, audiences and signing keys to configuration, in the Authentication:Schemes:Bearer
//   section. appsettings.Development.json and `dotnet user-jwts` fill it in when you run the app, and
//   the tests give their own. In the options lambda, keep the claims' names as the token has them:
//   set MapInboundClaims to false, and TokenValidationParameters.NameClaimType to "sub" and
//   RoleClaimType to "role". Those three can't be set from configuration.

// TODO 2: In builder.Services.AddAuthorization(options => ...), set a FallbackPolicy that requires an
//   authenticated user, so that every endpoint is private unless it says otherwise. Then mark /health,
//   below, with .AllowAnonymous(): load balancers call it with no token.

// TODO 3: Add a "PlaceOrders" policy: the caller's token must grant the orders:write scope. Identity
//   providers send the scopes as one space-separated string in the `scope` claim
//   ("orders:read orders:write"), so RequireClaim("scope", "orders:write") isn't enough. The token must
//   have a `sub` too, because it becomes the order's owner: add RequireClaim("sub"). Then require the
//   policy on POST /orders with .RequireAuthorization("PlaceOrders").

// TODO 4: Customers may read only their own orders, and the support role may read any order. That
//   needs the order, so it can't be a policy on the endpoint alone. Write a requirement (a class that
//   implements IAuthorizationRequirement) and its handler (AuthorizationHandler<TRequirement, Order>),
//   which succeeds when the user's `sub` is the order's CustomerId, or the user is in the support role.
//   Register the handler as an IAuthorizationHandler, and add a "ReadOrder" policy with the
//   requirement. Then, in GET /orders/{id}, once the order is loaded, call
//   IAuthorizationService.AuthorizeAsync(user, order, "ReadOrder"), and return Results.Forbid() if it
//   fails.

// TODO 5: Add a "Refunds" policy that requires the support role, and require it on the refund endpoint.

var app = builder.Build();

app.MapGet("/health", () => "ok");

// No authorization metadata of its own: once TODO 2 is done, the fallback policy applies.
app.MapGet("/catalogue", () => Catalogue.PricesCents);

// Who the caller is: Identity.Name comes from the claim that NameClaimType names (TODO 1).
app.MapGet("/me", (ClaimsPrincipal user) => new { name = user.Identity?.Name });

app.MapPost("/orders", (PlaceOrderRequest request, ClaimsPrincipal user, OrderStore store) =>
{
    var problems = request.Problems();
    if (problems.Count > 0)
    {
        return Results.ValidationProblem(problems);
    }

    var order = store.Add(user.FindFirstValue("sub") ?? "anonymous", request.Lines!);
    return Results.Created($"/orders/{order.Id}", order);
});

app.MapGet("/orders/{id:int}", (int id, OrderStore store) =>
    store.Find(id) is { } order ? Results.Ok(order) : Results.NotFound());

app.MapPost("/orders/{id:int}/refund", (int id, OrderStore store) =>
{
    if (store.Find(id) is not { } order)
    {
        return Results.NotFound();
    }

    store.Update(order with { Status = OrderStatus.Refunded });
    return Results.NoContent();
});

app.Run();

/// <summary>Visible to the tests' <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
