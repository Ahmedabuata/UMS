using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Services.Security;
using University.Infrastructure.Data;
using University.Shared.Common;

namespace University.Infrastructure.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly ApplicationDbContext _context;

    public RolePermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role == null)
        {
            return Result<bool>.NotFound("ROLE_NOT_FOUND", "Role not found.");
        }

        var permission = await _context.Permissions.FindAsync(permissionId);
        if (permission == null)
        {
            return Result<bool>.NotFound("PERMISSION_NOT_FOUND", "Permission not found.");
        }

        var exists = await _context.RolePermissions.AnyAsync(rp =>
            rp.RoleId == roleId && rp.PermissionId == permissionId && rp.IsActive);
        if (exists)
        {
            return Result<bool>.Conflict("ALREADY_ASSIGNED", "Permission is already assigned to this role.");
        }

        _context.RolePermissions.Add(new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            GrantedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var rp = await _context.RolePermissions.FirstOrDefaultAsync(link =>
            link.RoleId == roleId && link.PermissionId == permissionId && link.IsActive);
        if (rp == null)
        {
            return Result<bool>.NotFound("NOT_ASSIGNED", "Permission is not assigned to this role.");
        }

        rp.IsActive = false;
        rp.UpdatedAt = DateTime.UtcNow;
        _context.RolePermissions.Update(rp);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<Guid>>> GetRolePermissionsAsync(Guid roleId)
    {
        var ids = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId && rp.IsActive)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        return Result<IReadOnlyList<Guid>>.Success(ids);
    }
}
