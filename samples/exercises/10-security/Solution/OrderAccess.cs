using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace SecureOrders;

/// <summary>The names of the app's authorization policies.</summary>
public static class Policies
{
    public const string PlaceOrders = "PlaceOrders";
    public const string ReadOrder = "ReadOrder";
    public const string Refunds = "Refunds";
}

/// <summary>The caller may read the order: it's theirs, or they're support staff.</summary>
public sealed class OrderAccessRequirement : IAuthorizationRequirement;

/// <summary>
/// Decides <see cref="OrderAccessRequirement"/> for one order. It needs the order itself, so it
/// runs from the endpoint, after the order is loaded, through <c>IAuthorizationService</c>.
/// </summary>
public sealed class OrderAccessHandler : AuthorizationHandler<OrderAccessRequirement, Order>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OrderAccessRequirement requirement, Order resource)
    {
        if (context.User.FindFirstValue("sub") == resource.CustomerId || context.User.IsInRole("support"))
        {
            context.Succeed(requirement);
        }

        // Not calling Fail() leaves the decision to other handlers; with none, the check fails.
        return Task.CompletedTask;
    }
}
