using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Services.Security;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.Infrastructure.Services;

public class SecurityGroupService : ISecurityGroupService
{
    private readonly ApplicationDbContext _context;

    public SecurityGroupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<SecurityGroupDto>>> GetAllAsync(string? branchCode = null)
    {
        var query = _context.Groups.Where(g => g.IsActive);
        if (!string.IsNullOrWhiteSpace(branchCode))
        {
            query = query.Where(g => g.BranchCode == null || g.BranchCode == branchCode);
        }

        var groups = await query.OrderBy(g => g.Name).ToListAsync();
        return Result<IEnumerable<SecurityGroupDto>>.Success(groups.Select(ToDto));
    }

    public async Task<Result<SecurityGroupDto>> GetByIdAsync(Guid id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null)
        {
            return Result<SecurityGroupDto>.NotFound("GROUP_NOT_FOUND", "Group not found.");
        }

        return Result<SecurityGroupDto>.Success(ToDto(group));
    }

    public async Task<Result<SecurityGroupDto>> CreateAsync(CreateSecurityGroupDto dto)
    {
        var nameExists = await _context.Groups.AnyAsync(g =>
            g.Name == dto.Name.Trim() && g.IsActive);
        if (nameExists)
        {
            return Result<SecurityGroupDto>.Conflict("GROUP_EXISTS", "A group with this name already exists.");
        }

        var group = new Group
        {
            Name = dto.Name.Trim(),
            DisplayName = dto.DisplayName ?? dto.Name.Trim(),
            Description = dto.Description,
            BranchCode = dto.BranchCode,
            IsActive = true
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        return Result<SecurityGroupDto>.Success(ToDto(group));
    }

    public async Task<Result<SecurityGroupDto>> UpdateAsync(Guid id, UpdateSecurityGroupDto dto)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null)
        {
            return Result<SecurityGroupDto>.NotFound("GROUP_NOT_FOUND", "Group not found.");
        }

        if (dto.DisplayName != null) group.DisplayName = dto.DisplayName;
        if (dto.Description != null) group.Description = dto.Description;
        if (dto.IsActive.HasValue) group.IsActive = dto.IsActive.Value;
        group.UpdatedAt = DateTime.UtcNow;

        _context.Groups.Update(group);
        await _context.SaveChangesAsync();

        return Result<SecurityGroupDto>.Success(ToDto(group));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null)
        {
            return Result<bool>.NotFound("GROUP_NOT_FOUND", "Group not found.");
        }

        group.IsActive = false;
        group.UpdatedAt = DateTime.UtcNow;
        _context.Groups.Update(group);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<SecurityUserDto>>> GetMembersAsync(Guid id)
    {
        var exists = await _context.Groups.AnyAsync(g => g.Id == id && g.IsActive);
        if (!exists)
        {
            return Result<IEnumerable<SecurityUserDto>>.NotFound("GROUP_NOT_FOUND", "Group not found.");
        }

        var userIds = await _context.UserGroups
            .Where(ug => ug.GroupId == id && ug.IsActive)
            .Select(ug => ug.UserId)
            .ToListAsync();

        var users = userIds.Count == 0
            ? new List<SecurityUserDto>()
            : await _context.Users
                .Include(u => u.Role)
                .Where(u => userIds.Contains(u.Id))
                .Select(u => ToUserDto(u))
                .ToListAsync();

        return Result<IEnumerable<SecurityUserDto>>.Success(users);
    }

    public async Task<Result<bool>> AddMemberAsync(Guid id, Guid userId)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null)
        {
            return Result<bool>.NotFound("GROUP_NOT_FOUND", "Group not found.");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        var already = await _context.UserGroups.AnyAsync(ug =>
            ug.UserId == userId && ug.GroupId == id && ug.IsActive);
        if (already)
        {
            return Result<bool>.Conflict("ALREADY_MEMBER", "User is already a member of this group.");
        }

        _context.UserGroups.Add(new UserGroup
        {
            UserId = userId,
            GroupId = id,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> RemoveMemberAsync(Guid id, Guid userId)
    {
        var ug = await _context.UserGroups.FirstOrDefaultAsync(link =>
            link.UserId == userId && link.GroupId == id && link.IsActive);
        if (ug == null)
        {
            return Result<bool>.NotFound("NOT_MEMBER", "User is not a member of this group.");
        }

        ug.IsActive = false;
        ug.UpdatedAt = DateTime.UtcNow;
        _context.UserGroups.Update(ug);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    private static SecurityGroupDto ToDto(Group group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        DisplayName = group.DisplayName ?? group.Name,
        Description = group.Description,
        IsActive = group.IsActive,
        BranchCode = group.BranchCode
    };

    private static SecurityUserDto ToUserDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        RoleName = u.Role?.RoleName ?? string.Empty,
        IsActive = u.IsActive,
        MustChangePassword = u.MustChangePassword,
        CreatedAt = u.CreatedAt
    };
}
