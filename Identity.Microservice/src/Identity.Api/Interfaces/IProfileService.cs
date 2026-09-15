using Identity.Api.DTOs;

namespace Identity.Api.Interfaces;

/// <summary>
/// Defines the contract for user profile management operations.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Retrieves a user profile by user ID.
    /// </summary>
    Task<UserProfileDto?> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a user profile (upsert).
    /// </summary>
    Task<UserProfileDto> UpsertProfileAsync(
        Guid userId,
        UpsertUserProfileRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        string? branchCode = null,
        Guid? createdBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user profile.
    /// </summary>
    Task<bool> DeleteProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user profile exists.
    /// </summary>
    Task<bool> ProfileExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}