using Microsoft.AspNetCore.Authorization;

namespace University.API.Attributes;

/// <summary>
/// Shorthand for [Authorize(Policy = "SuperAdminOnly")].
/// Grants access only to principals carrying the SUPER_ADMIN role.
/// Used to harden destructive operations (e.g. DELETE).
/// Example: [SuperAdminOnly]
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SuperAdminOnlyAttribute : AuthorizeAttribute
{
    private const string PolicyName = "SuperAdminOnly";

    public SuperAdminOnlyAttribute()
        : base(PolicyName)
    {
    }
}
