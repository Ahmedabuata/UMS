using Identity.Api.DTOs;

namespace Identity.Api.Interfaces;

/// <summary>
/// Defines the contract for user management operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of users with optional search filtering.
    /// </summary>
    Task<UserListResponse> GetUsersAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user with the specified details.
    /// </summary>
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user's details.
    /// </summary>
    Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the active status of a user.
    /// </summary>
    Task<UserDto> PatchUserStatusAsync(Guid userId, PatchUserStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a user by deactivating their account and revoking all tokens.
    /// </summary>
    Task<bool> SoftDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    Task AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task UnassignRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a group to a user.
    /// </summary>
    Task AssignGroupToUserAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a group from a user.
    /// </summary>
    Task UnassignGroupFromUserAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all roles assigned to a user.
    /// </summary>
    Task<List<RoleDto>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all groups assigned to a user.
    /// </summary>
    Task<List<GroupDto>> GetUserGroupsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the profile of a user.
    /// </summary>
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}