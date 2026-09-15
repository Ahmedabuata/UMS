using Identity.Api.DTOs;

namespace Identity.Api.Interfaces;

/// <summary>
/// Defines the contract for permission management operations.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Retrieves all permissions with optional search and pagination.
    /// </summary>
    Task<List<PermissionDto>> GetAllPermissionsAsync(
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a permission by its unique identifier.
    /// </summary>
    Task<PermissionDto?> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing permission.
    /// </summary>
    Task<PermissionDto> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a permission.
    /// </summary>
    Task<bool> DeletePermissionAsync(Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a permission exists by name.
    /// </summary>
    Task<bool> PermissionExistsAsync(string permissionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of roles assigned to a permission.
    /// </summary>
    Task<int> GetRoleCountForPermissionAsync(Guid permissionId, CancellationToken cancellationToken = default);
}