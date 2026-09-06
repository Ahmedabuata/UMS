using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IdentityDbContext _db;
    public RolesController(IdentityDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "RoleRead")]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _db.Roles
            .OrderBy(r => r.RoleName)
            .Select(r => new RoleDto(
                r.Id,
                r.RoleName,
                r.DisplayName,
                r.Description,
                r.BranchCode,
                r.IsSystemRole,
                r.IsActive,
                r.CreatedAt,
                r.UpdatedAt,
                _db.UserRoles.Count(ur => ur.RoleId == r.Id)
            ))
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "RoleRead")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var r = await _db.Roles.FindAsync(id);
        if (r == null) return NotFound();

        var count = await _db.UserRoles.CountAsync(ur => ur.RoleId == id);
        return Ok(new RoleDto(
            r.Id,
            r.RoleName,
            r.DisplayName,
            r.Description,
            r.BranchCode,
            r.IsSystemRole,
            r.IsActive,
            r.CreatedAt,
            r.UpdatedAt,
            count
        ));
    }

    [HttpPost]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RoleName)) 
            return BadRequest(new { message = "RoleName required" });

        var name = req.RoleName.Trim().ToUpperInvariant();
        if (await _db.Roles.AnyAsync(r => r.RoleName == name))
            return Conflict(new { message = "Role exists" });

        var role = new Role
        {
            Id = Guid.NewGuid(),
            RoleName = name,
            DisplayName = req.DisplayName?.Trim(),
            Description = req.Description?.Trim(),
            BranchCode = req.BranchCode,
            IsSystemRole = req.IsSystemRole,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = role.Id }, new RoleDto(
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
        ));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest req)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound();

        if (req.DisplayName != null) role.DisplayName = req.DisplayName.Trim();
        if (req.Description != null) role.Description = req.Description.Trim();
        if (req.BranchCode != null) role.BranchCode = req.BranchCode;
        if (req.IsActive.HasValue) role.IsActive = req.IsActive.Value;

        role.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var count = await _db.UserRoles.CountAsync(ur => ur.RoleId == id);
        return Ok(new RoleDto(
            role.Id,
            role.RoleName,
            role.DisplayName,
            role.Description,
            role.BranchCode,
            role.IsSystemRole,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt,
            count
        ));
    }
}