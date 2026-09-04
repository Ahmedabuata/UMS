using Microsoft.AspNetCore.Authorization;

namespace University.Infrastructure.Security;

/// <summary>
/// Policy requirement for the "HasPermission:&lt;PERMISSION&gt;" dynamic authorization policy.
/// Carries the specific permission name required to access the guarded endpoint.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The exact permission claim value required (e.g. "FINANCE_READ", "HR_EMPLOYEE_READ").
    /// When null, no specific permission is required and access is granted to any
    /// authenticated user carrying at least one permission claim or a privileged role.
    /// </summary>
    public string? RequiredPermission { get; }

    public PermissionRequirement(string? requiredPermission = null)
    {
        RequiredPermission = requiredPermission;
    }
}
