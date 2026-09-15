using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Identity.Api.Interfaces; 
namespace Identity.Api.Services;

/// <summary>
/// Implements group management operations.
/// </summary>
public class GroupService : IGroupService
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<GroupService> _logger;

    public GroupService(
        IdentityDbContext db,
        ITokenService tokenService,
        ILogger<GroupService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<GroupDto>> GetAllGroupsAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Groups.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(g =>
                g.Name.ToLower().Contains(s) ||
                (g.DisplayName != null && g.DisplayName.ToLower().Contains(s)) ||
                (g.Description != null && g.Description.ToLower().Contains(s)));
        }

        return await query
            .OrderBy(g => g.Name)
            .Select(g => new GroupDto(
                g.Id,
                g.Name,
                g.DisplayName ?? g.Name,
                g.Description,
                g.BranchCode,
                g.IsActive,
                g.CreatedAt,
                g.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GroupDto?> GetGroupByIdAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await _db.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
            return null;

        return new GroupDto(
            group.Id,
            group.Name,
            group.DisplayName ?? group.Name,
            group.Description,
            group.BranchCode,
            group.IsActive,
            group.CreatedAt,
            group.UpdatedAt
        );
    }

    /// <inheritdoc />
    public async Task<GroupDto> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Group name is required.");

        var name = request.Name.Trim();

        // Check duplicate
        if (await _db.Groups.AnyAsync(g => g.Name == name, cancellationToken))
            throw new InvalidOperationException($"Group '{name}' already exists.");

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = request.DisplayName?.Trim() ?? name,
            Description = request.Description?.Trim(),
            BranchCode = request.BranchCode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Groups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);

        return new GroupDto(
            group.Id,
            group.Name,
            group.DisplayName,
            group.Description,
            group.BranchCode,
            group.IsActive,
            group.CreatedAt,
            group.UpdatedAt
        );
    }

    /// <inheritdoc />
    public async Task<GroupDto> UpdateGroupAsync(Guid groupId, UpdateGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await _db.Groups.FindAsync(new object[] { groupId }, cancellationToken);
        if (group == null)
            throw new KeyNotFoundException("Group not found.");

        var oldValues = new { group.Name, group.DisplayName, group.Description, group.BranchCode, group.IsActive };

        // Update fields
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var newName = request.Name.Trim();
            if (await _db.Groups.AnyAsync(g => g.Name == newName && g.Id != groupId, cancellationToken))
                throw new InvalidOperationException($"Group '{newName}' already exists.");

            group.Name = newName;
        }

        if (request.DisplayName != null)
            group.DisplayName = request.DisplayName.Trim();

        if (request.Description != null)
            group.Description = request.Description.Trim();

        if (request.BranchCode != null)
            group.BranchCode = request.BranchCode;

        if (request.IsActive.HasValue)
            group.IsActive = request.IsActive.Value;

        group.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        // If group was changed, revoke tokens for all users in this group
        await RevokeTokensForGroupUsersAsync(groupId);

        return new GroupDto(
            group.Id,
            group.Name,
            group.DisplayName ?? group.Name,
            group.Description,
            group.BranchCode,
            group.IsActive,
            group.CreatedAt,
            group.UpdatedAt
        );
    }

    /// <inheritdoc />
    public async Task<bool> DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await _db.Groups.FindAsync(new object[] { groupId }, cancellationToken);
        if (group == null)
            return false;

        // Check if group has assigned users
        var userCount = await _db.UserGroups.CountAsync(ug => ug.GroupId == groupId, cancellationToken);
        if (userCount > 0)
            throw new InvalidOperationException("Cannot delete group with assigned users.");

        _db.Groups.Remove(group);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<UserDto>> GetGroupUsersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var groupExists = await _db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken);
        if (!groupExists)
            throw new KeyNotFoundException("Group not found.");

        var users = await _db.UserGroups
            .Where(ug => ug.GroupId == groupId && ug.User != null)
            .Select(ug => ug.User!)
            .ToListAsync(cancellationToken);

        // Map users to DTOs (simplified - you can use UserService's mapping if needed)
        return users.Select(u => new UserDto(
            u.Id,
            u.Username,
            u.Email,
            u.PhoneNumber,
            u.IsActive,
            u.MustChangePassword,
            u.FailedLoginAttempts,
            u.LockoutEnd,
            u.FirstName,
            u.LastName,
            u.CreatedAt,
            u.UpdatedAt,
            new List<string>(),
            new List<string>(),
            new List<string>()
        )).ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetUserCountForGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _db.UserGroups.CountAsync(ug => ug.GroupId == groupId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> GroupExistsAsync(string groupName, CancellationToken cancellationToken = default)
    {
        return await _db.Groups.AnyAsync(g => g.Name == groupName.Trim(), cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Revokes all active tokens for users assigned to a specific group.
    /// </summary>
    private async Task RevokeTokensForGroupUsersAsync(Guid groupId)
    {
        try
        {
            var userIds = await _db.UserGroups
                .Where(ug => ug.GroupId == groupId)
                .Select(ug => ug.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in userIds)
            {
                await _tokenService.RevokeAllUserTokensAsync(userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke tokens for users in group {GroupId}", groupId);
        }
    }

    #endregion
}