using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Services.Security;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.Permissions;

namespace University.Infrastructure.Services;

public class SecurityPermissionService : ISecurityPermissionService
{
    private readonly ApplicationDbContext _context;

    public SecurityPermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> HasPermissionAsync(Guid userId, string permissionKey)
    {
        var codes = await GetPermissionCodesAsync(userId);
        return Result<bool>.Success(codes.Contains(permissionKey.Trim(), StringComparer.OrdinalIgnoreCase));
    }

    public async Task<Result<IReadOnlyList<string>>> GetUserPermissionsAsync(Guid userId)
    {
        var codes = await GetPermissionCodesAsync(userId);
        return Result<IReadOnlyList<string>>.Success(codes);
    }

    public async Task<Result<PermissionResponseDto>> CreateAsync(CreatePermissionRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PermissionName))
        {
            return Result<PermissionResponseDto>.Validation("PERMISSION_NAME_REQUIRED", "Permission name is required.");
        }

        var name = dto.PermissionName.Trim().ToUpperInvariant();
        if (await _context.Permissions.AnyAsync(p => p.PermissionName == name))
        {
            return Result<PermissionResponseDto>.Conflict("PERMISSION_EXISTS", "A permission with this name already exists.");
        }

        var permission = new Permission
        {
            PermissionName = name,
            Description = dto.Description,
            Module = string.IsNullOrWhiteSpace(dto.Module) ? "Other" : dto.Module.Trim(),
            ModuleCode = string.IsNullOrWhiteSpace(dto.ModuleCode) ? null : dto.ModuleCode.Trim().ToUpperInvariant(),
            IsActive = true
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        return Result<PermissionResponseDto>.Success(ToDto(permission));
    }

    public async Task<Result<PermissionResponseDto>> UpdateAsync(Guid id, UpdatePermissionRequestDto dto)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null)
        {
            return Result<PermissionResponseDto>.NotFound("PERMISSION_NOT_FOUND", "Permission not found.");
        }

        if (!string.IsNullOrWhiteSpace(dto.PermissionName))
        {
            var newName = dto.PermissionName.Trim().ToUpperInvariant();
            var duplicate = await _context.Permissions
                .AnyAsync(p => p.PermissionName == newName && p.Id != id);
            if (duplicate)
            {
                return Result<PermissionResponseDto>.Conflict("PERMISSION_EXISTS", "A permission with this name already exists.");
            }
            permission.PermissionName = newName;
        }

        if (!string.IsNullOrWhiteSpace(dto.Module))
        {
            permission.Module = dto.Module.Trim();
        }
        if (dto.ModuleCode != null)
        {
            permission.ModuleCode = string.IsNullOrWhiteSpace(dto.ModuleCode)
                ? null
                : dto.ModuleCode.Trim().ToUpperInvariant();
        }
        if (dto.Description != null)
        {
            permission.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        }

        permission.UpdatedAt = DateTime.UtcNow;
        _context.Permissions.Update(permission);
        await _context.SaveChangesAsync();

        return Result<PermissionResponseDto>.Success(ToDto(permission));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null)
        {
            return Result<bool>.NotFound("PERMISSION_NOT_FOUND", "Permission not found.");
        }

        var isAssigned = await _context.RolePermissions
            .AnyAsync(rp => rp.PermissionId == id && rp.IsActive);
        if (isAssigned)
        {
            return Result<bool>.Conflict("PERMISSION_IN_USE", "This permission is assigned to one or more roles and cannot be deleted.");
        }

        permission.IsActive = false;
        permission.UpdatedAt = DateTime.UtcNow;
        _context.Permissions.Update(permission);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    private static PermissionResponseDto ToDto(Permission p)
    {
        return new PermissionResponseDto
        {
            Id = p.Id,
            PermissionName = p.PermissionName,
            Description = p.Description,
            Module = p.Module,
            IsActive = p.IsActive
        };
    }

    private async Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid userId)
    {
        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId && ur.IsActive)
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToListAsync();

        var legacyRoleId = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => (Guid?)u.RoleId)
            .FirstOrDefaultAsync();

        if (legacyRoleId.HasValue)
        {
            roleIds.Add(legacyRoleId.Value);
        }

        roleIds = roleIds.Distinct().ToList();

        if (roleIds.Count == 0)
        {
            return Array.Empty<string>();
        }

        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsActive)
            .Select(rp => rp.Permission!.PermissionName)
            .Distinct()
            .ToListAsync();
    }
}