using System.Text.Json;
using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Identity.Api.Interfaces; 
namespace Identity.Api.Services;

/// <summary>
/// Implements user profile management operations.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        IdentityDbContext db,
        ITokenService tokenService,
        IAuditService auditService,
        ILogger<ProfileService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserProfileDto?> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile == null)
            return null;

        return new UserProfileDto(
            profile.Id,
            profile.UserId,
            profile.Address,
            profile.City,
            profile.PostalCode,
            profile.MaritalStatus,
            profile.DateOfBirth,
            profile.Gender,
            profile.CreatedAt,
            profile.UpdatedAt
        );
    }

    /// <inheritdoc />
    public async Task<UserProfileDto> UpsertProfileAsync(
        Guid userId,
        UpsertUserProfileRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        string? branchCode = null,
        Guid? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        // Validate
        if (request.DateOfBirth.HasValue && request.DateOfBirth.Value > DateTime.UtcNow)
            throw new ArgumentException("Date of birth cannot be in the future.");

        var allowedGenders = new[] { "Male", "Female", "Other" };
        if (!string.IsNullOrWhiteSpace(request.Gender) &&
            !allowedGenders.Contains(request.Gender, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid gender value. Allowed values are Male, Female, Other.");
        }

        // Check if user exists
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            throw new KeyNotFoundException($"User with ID '{userId}' not found.");

        var profile = await _db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        bool isNew = profile == null;

        if (isNew)
        {
            // Create new profile
            profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Address = request.Address,
                City = request.City,
                PostalCode = request.PostalCode,
                MaritalStatus = request.MaritalStatus,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.UserProfiles.Add(profile);

            // Revoke tokens when profile is created
            await _tokenService.RevokeAllUserTokensAsync(userId);
        }
        else
        {
            // Update existing profile
            var oldValues = new
            {
                profile.Address,
                profile.City,
                profile.PostalCode,
                profile.MaritalStatus,
                profile.DateOfBirth,
                profile.Gender
            };

            profile.Address = request.Address;
            profile.City = request.City;
            profile.PostalCode = request.PostalCode;
            profile.MaritalStatus = request.MaritalStatus;
            profile.DateOfBirth = request.DateOfBirth;
            profile.Gender = request.Gender;
            profile.UpdatedAt = DateTime.UtcNow;

            // Revoke tokens when profile is updated
            await _tokenService.RevokeAllUserTokensAsync(userId);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditService.LogAsync(
            action: isNew ? "CREATE_PROFILE" : "UPDATE_PROFILE",
            userId: userId,
            entityId: profile.Id.ToString(),
            entity: "UserProfile",
            tableName: "user_profiles",
            oldValues: isNew ? null : new { Address = profile.Address, City = profile.City, Gender = profile.Gender },
            newValues: new { request.Address, request.City, request.Gender },
            ipAddress: ipAddress,
            userAgent: userAgent,
            branchCode: branchCode,
            createdBy: createdBy,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation(
            "Profile {Action} for user {UserId}",
            isNew ? "created" : "updated",
            userId
        );

        return new UserProfileDto(
            profile.Id,
            profile.UserId,
            profile.Address,
            profile.City,
            profile.PostalCode,
            profile.MaritalStatus,
            profile.DateOfBirth,
            profile.Gender,
            profile.CreatedAt,
            profile.UpdatedAt
        );
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile == null)
            return false;

        // Audit log before deletion
        await _auditService.LogAsync(
            action: "DELETE_PROFILE",
            userId: userId,
            entityId: profile.Id.ToString(),
            entity: "UserProfile",
            tableName: "user_profiles",
            oldValues: new { profile.Address, profile.City, profile.Gender },
            ipAddress: null,
            userAgent: null,
            branchCode: null,
            createdBy: null,
            cancellationToken: cancellationToken
        );

        _db.UserProfiles.Remove(profile);

        // Revoke tokens when profile is deleted
        await _tokenService.RevokeAllUserTokensAsync(userId);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Profile deleted for user {UserId}", userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ProfileExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.UserProfiles.AnyAsync(p => p.UserId == userId, cancellationToken);
    }
}