using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SecureOrders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OrderStore>();

// TODO 1 (solved): JWT bearer authentication. The issuer, audiences and signing keys come from the
// Authentication:Schemes:Bearer section of configuration: appsettings.Development.json and the key
// `dotnet user-jwts` keeps in user secrets, or the tests' own settings. The claim settings can't be
// set from configuration, so they're here: keep the token's claim names (`sub`, `role`, `scope`).
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters.NameClaimType = "sub";
    options.TokenValidationParameters.RoleClaimType = "role";
});

builder.Services.AddAuthorization(options =>
{
    // TODO 2 (solved): every endpoint needs a signed-in caller unless it says otherwise.
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

    // TODO 3 (solved): placing an order needs the orders:write scope.
    options.AddPolicy(Policies.PlaceOrders, policy => policy.RequireAssertion(context => context.User.HasScope(Scopes.WriteOrders)));

    // TODO 4 (solved): reading an order needs the order itself; OrderAccessHandler decides.
    options.AddPolicy(Policies.ReadOrder, policy => policy.AddRequirements(new OrderAccessRequirement()));

    // TODO 5 (solved): refunds are for support staff only.
    options.AddPolicy(Policies.Refunds, policy => policy.RequireRole("support"));
});
builder.Services.AddSingleton<IAuthorizationHandler, OrderAccessHandler>();

var app = builder.Build();

// TODO 2 (solved): health checks come from load balancers, with no token.
app.MapGet("/health", () => "ok").AllowAnonymous();

// No authorization metadata of its own: the fallback policy applies.
app.MapGet("/catalogue", () => Catalogue.PricesCents);

app.MapPost("/orders", (PlaceOrderRequest request, ClaimsPrincipal user, OrderStore store) =>
{
    var problems = request.Problems();
    if (problems.Count > 0)
    {
        return Results.ValidationProblem(problems);
    }

    var order = store.Add(user.FindFirstValue("sub") ?? "anonymous", request.Lines!);
    return Results.Created($"/orders/{order.Id}", order);
}).RequireAuthorization(Policies.PlaceOrders);

app.MapGet("/orders/{id:int}", async (int id, ClaimsPrincipal user, OrderStore store, IAuthorizationService authorization) =>
{
    if (store.Find(id) is not { } order)
    {
        return Results.NotFound();
    }

    var allowed = await authorization.AuthorizeAsync(user, order, Policies.ReadOrder);
    return allowed.Succeeded ? Results.Ok(order) : Results.Forbid();
});

app.MapPost("/orders/{id:int}/refund", (int id, OrderStore store) =>
{
    if (store.Find(id) is not { } order)
    {
        return Results.NotFound();
    }

    store.Update(order with { Status = OrderStatus.Refunded });
    return Results.NoContent();
}).RequireAuthorization(Policies.Refunds);

app.Run();

/// <summary>Visible to the tests' <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
