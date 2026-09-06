using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    public PermissionsController(IdentityDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "PermissionRead")]
    public async Task<IActionResult> GetAll() 
        => Ok(await _db.Permissions
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
                p.UpdatedAt))
            .ToListAsync());

    [HttpGet("{id}")]
    [Authorize(Policy = "PermissionRead")]
    public async Task<IActionResult> GetById(Guid id)
    { 
        var p = await _db.Permissions.FindAsync(id); 
        if (p == null) return NotFound(); 
        return Ok(new PermissionDto(
            p.Id, 
            p.PermissionName, 
            p.Description, 
            p.Module, 
            p.ModuleCode, 
            p.BranchCode, 
            p.IsSensitive, 
            p.IsActive, 
            p.CreatedAt, 
            p.UpdatedAt)); 
    }

    [HttpPost]
    [Authorize(Policy = "PermissionWrite")]
    public async Task<IActionResult> Create([FromBody] CreatePermissionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.PermissionName)) 
            return BadRequest(new { message = "PermissionName required" });

        var name = req.PermissionName.Trim().ToUpperInvariant();
        if (await _db.Permissions.AnyAsync(x => x.PermissionName == name)) 
            return Conflict(new { message = "Permission exists" });

        var perm = new Permission 
        { 
            Id = Guid.NewGuid(), 
            PermissionName = name, 
            Description = req.Description?.Trim(),
            Module = req.Module,
            ModuleCode = req.ModuleCode,
            BranchCode = req.BranchCode,
            IsSensitive = req.IsSensitive,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Permissions.Add(perm); 
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = perm.Id }, new PermissionDto(
            perm.Id, 
            perm.PermissionName, 
            perm.Description, 
            perm.Module, 
            perm.ModuleCode, 
            perm.BranchCode, 
            perm.IsSensitive, 
            perm.IsActive, 
            perm.CreatedAt, 
            perm.UpdatedAt));
    }
}