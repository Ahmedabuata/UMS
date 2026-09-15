namespace Identity.Api.DTOs;

// ============================================================
// CREATE ROLE REQUEST
// ============================================================
/// <summary>
/// Request for creating a new role.
/// 
/// RoleName:     Auto-generated from DisplayName (e.g., "HR MANAGER" → "HR_MANAGER").
///               For developers/programmatic use.
/// DisplayName:  Human-readable name shown in UI (e.g., "HR MANAGER").
/// Description:  Optional - role description.
/// BranchCode:   Optional - scope this role to a specific branch.
/// IsSystemRole: System roles cannot be deleted (e.g., SUPER_ADMIN, USER).
/// IsActive:     Whether the role is active (default: true) - Required by DB NOT NULL.
/// </summary>
public record CreateRoleRequest(
    string RoleName,
    string DisplayName,
    string? Description = null,
    string? BranchCode = null,
    bool IsSystemRole = false,
    bool IsActive = true
);

// ============================================================
// UPDATE ROLE REQUEST
// ============================================================
/// <summary>
/// Request for updating an existing role.
/// All fields optional (PATCH semantics via PUT).
/// 
/// RoleName can be updated (though usually auto-generated).
/// IsSystemRole can be toggled (only by admins).
/// </summary>
public record UpdateRoleRequest(
    string? RoleName = null,
    string? DisplayName = null,
    string? Description = null,
    string? BranchCode = null,
    bool? IsSystemRole = null,
    bool? IsActive = null
);

// ============================================================
//  NEW: PATCH ROLE STATUS REQUEST
// ============================================================
/// <summary>
/// Request for quick activate/deactivate of a role.
/// 
/// Used by PATCH /api/roles/{id}/status
/// 
/// Business Rules:
/// - SUPER_ADMIN cannot be deactivated.
/// - No other fields can be modified via this endpoint.
/// 
/// Example:
///   PATCH /api/roles/{id}/status
///   { "isActive": true }   → activates the role
///   { "isActive": false }  → deactivates the role
/// </summary>
public record PatchRoleStatusRequest(bool IsActive);

// ============================================================
// ROLE DTO (Response)
// ============================================================
/// <summary>
/// Role data transfer object - returned to frontend.
/// Contains all fields the frontend needs to display/edit a role.
/// </summary>
public record RoleDto(
    Guid Id,
    string RoleName,
    string DisplayName,
    string? Description = null,
    string? BranchCode = null,
    bool IsSystemRole = false,
    bool IsActive = true,
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default,
    int UserCount = 0
);