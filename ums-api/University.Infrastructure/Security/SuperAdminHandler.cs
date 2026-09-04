using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace University.Infrastructure.Security;

/// <summary>
/// Handler for the "SuperAdminOnly" authorization policy.
/// The principal must belong to the SUPER_ADMIN role to succeed.
/// </summary>
public class SuperAdminHandler : AuthorizationHandler<SuperAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SuperAdminRequirement requirement)
    {
        var user = context.User;
        if (user.Identity != null && user.Identity.IsAuthenticated &&
            (user.IsInRole("SUPER_ADMIN")
             || user.HasClaim("role", "SUPER_ADMIN")
             || user.FindFirstValue("role")?.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) == true))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
