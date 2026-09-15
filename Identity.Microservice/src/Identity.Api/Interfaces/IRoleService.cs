using Identity.Api.DTOs;

namespace Identity.Api.Interfaces;

/// <summary>
/// Defines the contract for role management operations.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Retrieves all roles with optional search and pagination.
    /// </summary>
    Task<List<RoleDto>> GetAllRolesAsync(
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a role by its unique identifier.
    /// </summary>
    Task<RoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role.
    /// </summary>
    Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all permissions assigned to a role.
    /// </summary>
    Task<List<PermissionDto>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a permission to a role.
    /// </summary>
    Task AssignPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a permission from a role.
    /// </summary>
    Task RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes permissions for a role (bulk update).
    /// </summary>
    Task BulkSyncPermissionsAsync(Guid roleId, List<Guid> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role exists by name.
    /// </summary>
    Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of users assigned to a role.
    /// </summary>
    Task<int> GetUserCountForRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}