using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace University.Infrastructure.Security;

/// <summary>
/// Handler for the "HasPermission:&lt;PERMISSION&gt;" dynamic authorization policy.
/// The principal is granted access only when they hold the exact required permission
/// claim (issued into the JWT by AuthService.GetPermissionsAsync), OR belong to a
/// privileged role (ADMIN / SUPER_ADMIN) that bypasses the fine-grained check.
/// </summary>
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var user = context.User;
        if (user.Identity == null || !user.Identity.IsAuthenticated)
        {
            return Task.CompletedTask;
        }

        // Privileged role bypass: ADMIN / SUPER_ADMIN short-circuit the fine-grained check.
        if (HasRole(user, "ADMIN") || HasRole(user, "SUPER_ADMIN"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Generic "HasPermission" policy (no specific permission): grant to any user with
        // at least one permission claim.
        if (string.IsNullOrEmpty(requirement.RequiredPermission))
        {
            if (user.FindAll("permission").Any())
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }

        // Strict fine-grained check: the principal must hold the exact required permission.
        if (user.FindAll("permission").Any(c => c.Value == requirement.RequiredPermission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool HasRole(ClaimsPrincipal user, string role) =>
        user.IsInRole(role)
        || user.HasClaim("role", role)
        || user.FindFirstValue("role")?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
}
