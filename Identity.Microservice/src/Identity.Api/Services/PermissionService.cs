using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Identity.Api.Interfaces; 
namespace Identity.Api.Services;

/// <summary>
/// Implements permission management operations.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IdentityDbContext db,
        ITokenService tokenService,
        ILogger<PermissionService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<PermissionDto>> GetAllPermissionsAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Permissions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                p.PermissionName.ToLower().Contains(s) ||
                (p.Description != null && p.Description.ToLower().Contains(s)) ||
                (p.Module != null && p.Module.ToLower().Contains(s)));
        }

        return await query
            .OrderBy(p => p.PermissionName)
            .Select(p => new PermissionDto(
                p.Id,
                p.PermissionName,
                p.Description,
                p.Module,
                p.ModuleCode,
                p.BranchCode,
                p.IsSensitive,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PermissionDto?> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await _db.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == permissionId, cancellationToken);

        if (permission == null)
            return null;

        return new PermissionDto(
            permission.Id,
            permission.PermissionName,
            permission.Description,
            permission.Module,
            permission.ModuleCode,
            permission.BranchCode,
            permission.IsSensitive,
            permission.IsActive,
            permission.CreatedAt,
            permission.UpdatedAt
        );
    }

    /// <inheritdoc />
    public async Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.PermissionName))
            throw new ArgumentException("PermissionName is required.");

        if (string.IsNullOrWhiteSpace(request.Module))
            throw new ArgumentException("Module is required.");

        var name = request.PermissionName.Trim().ToUpperInvariant();

        // Check duplicate
        if (await _db.Permissions.AnyAsync(p => p.PermissionName == name, cancellationToken))
            throw new InvalidOperationException($"Permission '{name}' already exists.");

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            PermissionName = name,
            Description = request.Description?.Trim() ?? name,
            Module = request.Module.Trim(),
            ModuleCode = request.ModuleCode?.Trim() ?? request.Module.Trim().ToUpperInvariant(),
            BranchCode = request.BranchCode,
            IsSensitive = request.IsSensitive,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync(cancellationToken);

        return new PermissionDto(
            permission.Id,
            permission.PermissionName,
            permission.Description,
            permission.Module,
            permission.ModuleCode,
            permission.BranchCode,
            permission.IsSensitive,
            permission.IsActive,
            permission.CreatedAt,
            permission.UpdatedAt
        );
    }

    /// <inheritdoc />
    public async Task<PermissionDto> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var permission = await _db.Permissions.FindAsync(new object[] { permissionId }, cancellationToken);
        if (permission == null)
            throw new KeyNotFoundException("Permission not found.");

        var oldValues = new
        {
            permission.Description,
            permission.Module,
            permission.ModuleCode,
            permission.BranchCode,
            permission.IsSensitive,
            permission.IsActive
        };

        // Update fields
        if (request.Description != null)
            permission.Description = request.Description.Trim();

        if (request.Module != null)
            permission.Module = request.Module.Trim();

        if (request.ModuleCode != null)
            permission.ModuleCode = request.ModuleCode.Trim();

        if (request.BranchCode != null)
            permission.BranchCode = request.BranchCode;

        if (request.IsSensitive.HasValue)
            permission.IsSensitive = request.IsSensitive.Value;

        if (request.IsActive.HasValue)
            permission.IsActive = request.IsActive.Value;

        permission.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        // Revoke tokens for all users with roles that have this permission
        await RevokeTokensForPermissionUsersAsync(permissionId);

        return new PermissionDto(
            permission.Id,
            permission.PermissionName,
            permission.Description,
            permission.Module,
            permission.ModuleCode,
            permission.BranchCode,
            permission.IsSensitive,
            permission.IsActive,
            permission.CreatedAt,
            permission.UpdatedAt
        );
    }

    /// <inheritdoc />
    public async Task<bool> DeletePermissionAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await _db.Permissions.FindAsync(new object[] { permissionId }, cancellationToken);
        if (permission == null)
            return false;

        // Check if permission is assigned to any role
        var roleCount = await _db.RolePermissions.CountAsync(rp => rp.PermissionId == permissionId, cancellationToken);
        if (roleCount > 0)
            throw new InvalidOperationException("Cannot delete permission assigned to one or more roles.");

        // Prevent deletion of sensitive permissions
        if (permission.IsSensitive)
            throw new InvalidOperationException("Cannot delete sensitive permission.");

        // Revoke tokens before deletion
        await RevokeTokensForPermissionUsersAsync(permissionId);

        _db.Permissions.Remove(permission);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> PermissionExistsAsync(string permissionName, CancellationToken cancellationToken = default)
    {
        return await _db.Permissions.AnyAsync(p => p.PermissionName == permissionName.Trim().ToUpperInvariant(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetRoleCountForPermissionAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _db.RolePermissions.CountAsync(rp => rp.PermissionId == permissionId, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Revokes all active tokens for users associated with roles that have a specific permission.
    /// </summary>
    private async Task RevokeTokensForPermissionUsersAsync(Guid permissionId)
    {
        try
        {
            var userIds = await _db.RolePermissions
                .Where(rp => rp.PermissionId == permissionId)
                .Join(_db.UserRoles, rp => rp.RoleId, ur => ur.RoleId, (rp, ur) => ur.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in userIds)
            {
                await _tokenService.RevokeAllUserTokensAsync(userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke tokens for users with permission {PermissionId}", permissionId);
        }
    }

    #endregion
}