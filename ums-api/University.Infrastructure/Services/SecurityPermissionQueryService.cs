using Microsoft.EntityFrameworkCore;
using University.Core.Interfaces.Services.Security;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.Infrastructure.Services;

public class SecurityPermissionQueryService : ISecurityPermissionQueryService
{
    private readonly ApplicationDbContext _context;

    public SecurityPermissionQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<PermissionItemDto>>> GetAllAsync(string? module = null)
    {
        var query = _context.Permissions.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(p => p.ModuleCode == module || p.Module == module);
        }

        var items = await query
            .OrderBy(p => p.ModuleCode)
            .ThenBy(p => p.PermissionName)
            .Select(p => new PermissionItemDto
            {
                Id = p.Id,
                Code = p.PermissionName,
                Name = p.PermissionName,
                Description = p.Description,
                ModuleCode = p.ModuleCode,
                Module = p.Module,
                IsSensitive = p.IsSensitive,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Result<IEnumerable<PermissionItemDto>>.Success(items);
    }

    public async Task<Result<IEnumerable<PermissionByModuleDto>>> GetGroupedByModuleAsync()
    {
        var groups = await _context.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.ModuleCode)
            .ThenBy(p => p.PermissionName)
            .ToListAsync();

        var byModule = groups
            .GroupBy(p => p.ModuleCode ?? p.Module ?? "OTHER")
            .Select(g => new PermissionByModuleDto
            {
                Module = g.First().Module,
                ModuleCode = g.Key,
                Permissions = g.Select(p => new PermissionItemDto
                {
                    Id = p.Id,
                    Code = p.PermissionName,
                    Name = p.PermissionName,
                    Description = p.Description,
                    ModuleCode = p.ModuleCode,
                    Module = p.Module,
                    IsSensitive = p.IsSensitive,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                }).ToList()
            })
            .ToList();

        return Result<IEnumerable<PermissionByModuleDto>>.Success(byModule);
    }

    public async Task<Result<IEnumerable<ModuleItemDto>>> GetModulesAsync()
    {
        var modules = await _context.Modules
            .Where(m => m.IsActive)
            .OrderBy(m => m.Code)
            .Select(m => new ModuleItemDto
            {
                Id = m.Id,
                Code = m.Code,
                Name = m.Name,
                Description = m.Description
            })
            .ToListAsync();

        return Result<IEnumerable<ModuleItemDto>>.Success(modules);
    }
}
