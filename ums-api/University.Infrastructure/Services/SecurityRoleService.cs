using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Services.Security;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.Infrastructure.Services;

public class SecurityRoleService : ISecurityRoleService
{
    private readonly ApplicationDbContext _context;

    public SecurityRoleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<SecurityRoleDto>>> GetAllAsync(string? branchCode = null)
    {
        var query = _context.Roles.Where(r => r.IsActive);
        if (!string.IsNullOrWhiteSpace(branchCode))
        {
            query = query.Where(r => r.BranchCode == null || r.BranchCode == branchCode);
        }

        var roles = await query
            .Include(r => r.RolePermissions)
            .OrderBy(r => r.RoleName)
            .ToListAsync();

        var dtos = new List<SecurityRoleDto>();
        foreach (var role in roles)
        {
            dtos.Add(await ToDtoAsync(role));
        }

        return Result<IEnumerable<SecurityRoleDto>>.Success(dtos);
    }

    public async Task<Result<SecurityRoleDto>> GetByIdAsync(Guid id)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role == null)
        {
            return Result<SecurityRoleDto>.NotFound("ROLE_NOT_FOUND", "Role not found.");
        }

        return Result<SecurityRoleDto>.Success(await ToDtoAsync(role));
    }

    public async Task<Result<SecurityRoleDto>> CreateAsync(CreateSecurityRoleDto dto)
    {
        var nameExists = await _context.Roles.AnyAsync(r =>
            r.RoleName == dto.Name.Trim() && r.IsActive);
        if (nameExists)
        {
            return Result<SecurityRoleDto>.Conflict("ROLE_EXISTS", "A role with this name already exists.");
        }

        var role = new Role
        {
            RoleName = dto.Name.Trim(),
            DisplayName = dto.DisplayName ?? dto.Name.Trim(),
            Description = dto.Description,
            BranchCode = dto.BranchCode,
            IsActive = true
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return Result<SecurityRoleDto>.Success(await ToDtoAsync(role));
    }

    public async Task<Result<SecurityRoleDto>> UpdateAsync(Guid id, UpdateSecurityRoleDto dto)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null)
        {
            return Result<SecurityRoleDto>.NotFound("ROLE_NOT_FOUND", "Role not found.");
        }

        if (dto.DisplayName != null) role.DisplayName = dto.DisplayName;
        if (dto.Description != null) role.Description = dto.Description;
        if (dto.IsActive.HasValue) role.IsActive = dto.IsActive.Value;
        role.UpdatedAt = DateTime.UtcNow;

        _context.Roles.Update(role);
        await _context.SaveChangesAsync();

        return Result<SecurityRoleDto>.Success(await ToDtoAsync(role));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null)
        {
            return Result<bool>.NotFound("ROLE_NOT_FOUND", "Role not found.");
        }

        if (role.IsSystemRole)
        {
            return Result<bool>.Forbidden("SYSTEM_ROLE", "System roles cannot be deleted.");
        }

        role.IsActive = false;
        role.UpdatedAt = DateTime.UtcNow;
        _context.Roles.Update(role);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<Guid>>> GetPermissionIdsAsync(Guid id)
    {
        var exists = await _context.Roles.AnyAsync(r => r.Id == id && r.IsActive);
        if (!exists)
        {
            return Result<IEnumerable<Guid>>.NotFound("ROLE_NOT_FOUND", "Role not found.");
        }

        var ids = await _context.RolePermissions
            .Where(rp => rp.RoleId == id && rp.IsActive)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        return Result<IEnumerable<Guid>>.Success(ids);
    }

    public async Task<Result<bool>> SetPermissionsAsync(Guid roleId, AssignPermissionsDto dto)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role == null)
        {
            return Result<bool>.NotFound("ROLE_NOT_FOUND", "Role not found.");
        }

        var existing = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        _context.RolePermissions.RemoveRange(existing);

        var requested = dto.PermissionIds.Distinct().ToList();
        if (requested.Count > 0)
        {
            var validIds = await _context.Permissions
                .Where(p => requested.Contains(p.Id) && p.IsActive)
                .Select(p => p.Id)
                .ToListAsync();

            _context.RolePermissions.AddRange(validIds.Select(pid => new RolePermission
            {
                RoleId = roleId,
                PermissionId = pid,
                GrantedAt = DateTime.UtcNow
            }));
        }

        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private async Task<SecurityRoleDto> ToDtoAsync(Role role)
    {
        var perms = await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == role.Id && rp.IsActive)
            .Select(rp => rp.Permission!.PermissionName)
            .Distinct()
            .ToListAsync();

        return new SecurityRoleDto
        {
            Id = role.Id,
            Name = role.RoleName,
            DisplayName = role.DisplayName ?? role.RoleName,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            IsActive = role.IsActive,
            BranchCode = role.BranchCode,
            Permissions = perms
        };
    }
}
