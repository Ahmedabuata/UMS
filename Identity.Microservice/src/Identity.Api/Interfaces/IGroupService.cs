using Identity.Api.DTOs;

namespace Identity.Api.Interfaces;

/// <summary>
/// Defines the contract for group management operations.
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// Retrieves all groups with optional search and pagination.
    /// </summary>
    Task<List<GroupDto>> GetAllGroupsAsync(
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a group by its unique identifier.
    /// </summary>
    Task<GroupDto?> GetGroupByIdAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new group.
    /// </summary>
    Task<GroupDto> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing group.
    /// </summary>
    Task<GroupDto> UpdateGroupAsync(Guid groupId, UpdateGroupRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a group.
    /// </summary>
    Task<bool> DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all users assigned to a group.
    /// </summary>
    Task<List<UserDto>> GetGroupUsersAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of users assigned to a group.
    /// </summary>
    Task<int> GetUserCountForGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a group exists by name.
    /// </summary>
    Task<bool> GroupExistsAsync(string groupName, CancellationToken cancellationToken = default);
}