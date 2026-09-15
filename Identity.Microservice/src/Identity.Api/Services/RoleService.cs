using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Identity.Api.Interfaces; 
namespace Identity.Api.Services;

/// <summary>
/// Implements role management operations.
/// </summary>
public class RoleService : IRoleService
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IdentityDbContext db,
        ITokenService tokenService,
        ILogger<RoleService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<RoleDto>> GetAllRolesAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Roles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r =>
                r.RoleName.ToLower().Contains(s) ||
                (r.DisplayName != null && r.DisplayName.ToLower().Contains(s)) ||
                (r.Description != null && r.Description.ToLower().Contains(s)));
        }

        return await query
            .OrderBy(r => r.RoleName)
            .Select(r => new RoleDto(
                r.Id,
                r.RoleName,
                r.DisplayName ?? r.RoleName,
                r.Description,
                r.BranchCode,
                r.IsSystemRole,
                r.IsActive,
                r.CreatedAt,
                r.UpdatedAt,
                _db.UserRoles.Count(ur => ur.RoleId == r.Id)
            ))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
            return null;

        var userCount = await _db.UserRoles.CountAsync(ur => ur.RoleId == roleId, cancellationToken);

        return new RoleDto(
            role.Id,
            role.RoleName,
            role.DisplayName ?? role.RoleName,
            role.Description,
            role.BranchCode,
            role.IsSystemRole,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt,
            userCount
        );
    }

    /// <inheritdoc />
    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.RoleName))
            throw new ArgumentException("RoleName is required.");

        var name = request.RoleName.Trim().ToUpperInvariant();

        // Check duplicate
        if (await _db.Roles.AnyAsync(r => r.RoleName == name, cancellationToken))
            throw new InvalidOperationException($"Role '{name}' already exists.");

        var role = new Role
        {
            Id = Guid.NewGuid(),
            RoleName = name,
            DisplayName = request.DisplayName?.Trim() ?? name,
            Description = request.Description?.Trim(),
            BranchCode = request.BranchCode,
            IsSystemRole = request.IsSystemRole,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);

        return new RoleDto(
            role.Id,
            role.RoleName,
            role.DisplayName,
            role.Description,
            role.BranchCode,
            role.IsSystemRole,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt,
            0
        );
    }

    /// <inheritdoc />
    public async Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FindAsync(new object[] { roleId }, cancellationToken);
        if (role == null)
            throw new KeyNotFoundException("Role not found.");

        var oldValues = new { role.DisplayName, role.Description, role.BranchCode, role.IsActive };

        // Update fields
        if (request.DisplayName != null)
            role.DisplayName = request.DisplayName.Trim();

        if (request.Description != null)
            role.Description = request.Description.Trim();

        if (request.BranchCode != null)
            role.BranchCode = request.BranchCode;

        if (request.IsActive.HasValue)
            role.IsActive = request.IsActive.Value;

        role.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        // If role was deactivated or changed, revoke tokens for all users with this role
        await RevokeTokensForRoleUsersAsync(roleId);

        var userCount = await _db.UserRoles.CountAsync(ur => ur.RoleId == roleId, cancellationToken);

        return new RoleDto(
            role.Id,
            role.RoleName,
            role.DisplayName ?? role.RoleName,
            role.Description,
            role.BranchCode,
            role.IsSystemRole,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt,
            userCount
        );
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FindAsync(new object[] { roleId }, cancellationToken);
        if (role == null)
            return false;

        // Check if role has assigned users
        var userCount = await _db.UserRoles.CountAsync(ur => ur.RoleId == roleId, cancellationToken);
        if (userCount > 0)
            throw new InvalidOperationException("Cannot delete role with assigned users.");

        // Prevent deletion of SUPER_ADMIN role
        if (role.RoleName == "SUPER_ADMIN")
            throw new InvalidOperationException("Cannot delete SUPER_ADMIN role.");

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(cancellationToken);

        // Revoke tokens for all users who had this role
        await RevokeTokensForRoleUsersAsync(roleId);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<PermissionDto>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var roleExists = await _db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
        if (!roleExists)
            throw new KeyNotFoundException("Role not found.");

        return await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId && rp.Permission != null)
            .Select(rp => new PermissionDto(
                rp.Permission!.Id,
                rp.Permission.PermissionName,
                rp.Permission.Description,
                rp.Permission.Module,
                rp.Permission.ModuleCode,
                rp.Permission.BranchCode,
                rp.Permission.IsSensitive,
                rp.Permission.IsActive,
                rp.Permission.CreatedAt,
                rp.Permission.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AssignPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var roleExists = await _db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
        if (!roleExists)
            throw new KeyNotFoundException("Role not found.");

        var permissionExists = await _db.Permissions.AnyAsync(p => p.Id == permissionId, cancellationToken);
        if (!permissionExists)
            throw new KeyNotFoundException("Permission not found.");

        var alreadyAssigned = await _db.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

        if (alreadyAssigned)
            throw new InvalidOperationException("Permission already assigned to role.");

        _db.RolePermissions.Add(new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId,
            IsActive = true,
            GrantedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        // Revoke tokens for all users with this role
        await RevokeTokensForRoleUsersAsync(roleId);
    }

    /// <inheritdoc />
    public async Task RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var rolePermission = await _db.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

        if (rolePermission == null)
            throw new KeyNotFoundException("Permission not assigned to role.");

        _db.RolePermissions.Remove(rolePermission);
        await _db.SaveChangesAsync(cancellationToken);

        // Revoke tokens for all users with this role
        await RevokeTokensForRoleUsersAsync(roleId);
    }

    /// <inheritdoc />
    public async Task BulkSyncPermissionsAsync(Guid roleId, List<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var roleExists = await _db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
        if (!roleExists)
            throw new KeyNotFoundException("Role not found.");

        var distinctPermIds = permissionIds.Distinct().ToList();

        // Validate all permissions exist
        var existingPermIds = await _db.Permissions
            .Where(p => distinctPermIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (existingPermIds.Count != distinctPermIds.Count)
            throw new ArgumentException("One or more permissions not found.");

        // Remove all current permissions
        var current = await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _db.RolePermissions.RemoveRange(current);

        // Add new permissions
        foreach (var pid in distinctPermIds)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                PermissionId = pid,
                IsActive = true,
                GrantedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Revoke tokens for all users with this role
        await RevokeTokensForRoleUsersAsync(roleId);
    }

    /// <inheritdoc />
    public async Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await _db.Roles.AnyAsync(r => r.RoleName == roleName.Trim().ToUpperInvariant(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetUserCountForRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _db.UserRoles.CountAsync(ur => ur.RoleId == roleId, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Revokes all active tokens for users assigned to a specific role.
    /// </summary>
    private async Task RevokeTokensForRoleUsersAsync(Guid roleId)
    {
        try
        {
            var userIds = await _db.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in userIds)
            {
                await _tokenService.RevokeAllUserTokensAsync(userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke tokens for users with role {RoleId}", roleId);
        }
    }

    #endregion
}