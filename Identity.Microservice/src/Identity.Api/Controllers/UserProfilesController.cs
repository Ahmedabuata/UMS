using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;          // ✅ أضف هذا
using Identity.Api.Services;
using Identity.Api.Interfaces;      // ✅ موجود
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Identity.Api.Controllers;

/// <summary>
/// Handles user profile management operations.
/// </summary>
[ApiController]
[Route("api/user-profiles")]
[Authorize]
public class UserProfilesController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserProfilesController> _logger;

    public UserProfilesController(
        IdentityDbContext db,
        ITokenService tokenService,
        ILogger<UserProfilesController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the user profile for a specific user by their ID.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [Authorize(Policy = "ProfileRead")]
    public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserIdStr = User?.FindFirst("userId")?.Value;
            var isAdmin = User?.IsInRole("ADMIN") ?? false;

            if (!isAdmin && Guid.TryParse(currentUserIdStr, out var parsedCurrentId) && parsedCurrentId != userId)
            {
                return StatusCode(403, new { code = "FORBIDDEN", message = "You can only view your own profile." });
            }

            var profile = await _db.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            if (profile == null)
            {
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "User profile not found." });
            }

            return Ok(new UserProfileDto(
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
            ));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The get profile request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving profile for user ID {UserId}.", userId);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// Creates or updates the user profile for a specific user.
    /// </summary>
    [HttpPut("{userId:guid}")]
    [Authorize(Policy = "ProfileWrite")]
    public async Task<IActionResult> UpsertProfile(Guid userId, [FromBody] UpsertUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserIdStr = User?.FindFirst("userId")?.Value;
            var isAdmin = User?.IsInRole("ADMIN") ?? false;

            if (!isAdmin && Guid.TryParse(currentUserIdStr, out var parsedCurrentId) && parsedCurrentId != userId)
            {
                return StatusCode(403, new { code = "FORBIDDEN", message = "You can only modify your own profile." });
            }

            if (request.DateOfBirth.HasValue && request.DateOfBirth.Value > DateTime.UtcNow)
            {
                return BadRequest(new { code = "INVALID_DATE_OF_BIRTH", message = "Date of birth cannot be in the future." });
            }

            var allowedGenders = new[] { "Male", "Female", "Other" };
            if (!string.IsNullOrWhiteSpace(request.Gender) && !allowedGenders.Contains(request.Gender, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { code = "INVALID_GENDER", message = "Invalid gender value. Allowed values are Male, Female, Other." });
            }

            var userExists = await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                return NotFound(new { code = "USER_NOT_FOUND", message = "The specified user does not exist." });
            }

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var profile = await _db.UserProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            var isNew = profile == null;

            if (isNew)
            {
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
            }
            else
            {
                profile.Address = request.Address;
                profile.City = request.City;
                profile.PostalCode = request.PostalCode;
                profile.MaritalStatus = request.MaritalStatus;
                profile.DateOfBirth = request.DateOfBirth;
                profile.Gender = request.Gender;
                profile.UpdatedAt = DateTime.UtcNow;
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = Guid.TryParse(currentUserIdStr, out var auditUid) ? auditUid : null,
                Action = isNew ? "CREATE_PROFILE" : "UPDATE_PROFILE",
                Entity = "UserProfile",
                EntityId = profile.Id.ToString(),
                TableName = "user_profiles",
                OldValues = isNew ? null : JsonSerializer.Serialize(new { profile.Address, profile.City, profile.Gender }),
                NewValues = JsonSerializer.Serialize(new { request.Address, request.City, request.Gender }),
                IpAddress = remoteIp,
                UserAgent = Request.Headers["User-Agent"].ToString(),
                BranchCode = User?.FindFirst("branchCode")?.Value,
                CreatedBy = Guid.TryParse(currentUserIdStr, out var createdBy) ? createdBy : null,
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });

            await _tokenService.RevokeAllUserTokensAsync(userId, remoteIp, currentUserIdStr);
            await _db.SaveChangesAsync(cancellationToken);

            var responseMessage = isNew ? "Profile created successfully." : "Profile updated successfully.";

            return Ok(new
            {
                message = responseMessage,
                data = new UserProfileDto(
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
                )
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The upsert profile request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while upserting profile for user ID {UserId}.", userId);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// Deletes the user profile for a specific user.
    /// </summary>
    [HttpDelete("{userId:guid}")]
    [Authorize(Policy = "ProfileDelete")]
    public async Task<IActionResult> DeleteProfile(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserIdStr = User?.FindFirst("userId")?.Value;
            var isAdmin = User?.IsInRole("ADMIN") ?? false;

            if (!isAdmin && Guid.TryParse(currentUserIdStr, out var parsedCurrentId) && parsedCurrentId != userId)
            {
                return StatusCode(403, new { code = "FORBIDDEN", message = "You can only delete your own profile." });
            }

            var profile = await _db.UserProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            if (profile == null)
            {
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "User profile not found." });
            }

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = Guid.TryParse(currentUserIdStr, out var auditUid) ? auditUid : null,
                Action = "DELETE_PROFILE",
                Entity = "UserProfile",
                EntityId = profile.Id.ToString(),
                TableName = "user_profiles",
                OldValues = JsonSerializer.Serialize(new { profile.Address, profile.City, profile.Gender }),
                IpAddress = remoteIp,
                UserAgent = Request.Headers["User-Agent"].ToString(),
                BranchCode = User?.FindFirst("branchCode")?.Value,
                CreatedBy = Guid.TryParse(currentUserIdStr, out var createdBy) ? createdBy : null,
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });

            _db.UserProfiles.Remove(profile);

            await _tokenService.RevokeAllUserTokensAsync(userId, remoteIp, currentUserIdStr);
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Profile deleted successfully." });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The delete profile request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting profile for user ID {UserId}.", userId);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred." });
        }
    }
}