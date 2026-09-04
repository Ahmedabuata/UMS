using Microsoft.AspNetCore.Authorization;

namespace University.Infrastructure.Security;

/// <summary>
/// Requirement for the "SuperAdminOnly" authorization policy.
/// Grants access only to principals carrying the SUPER_ADMIN role.
/// Used to harden destructive operations (e.g. DELETE) per the Golden Rule
/// that HR Manager (or any non-super-admin) cannot delete records.
/// </summary>
public class SuperAdminRequirement : IAuthorizationRequirement
{
}
