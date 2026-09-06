using DotNetCore.CAP;
using HR.Infrastructure.Data;
using HR.Shared.Contracts.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly HrDbContext _db;
    private readonly ICapPublisher _bus;
    public EmployeesController(HrDbContext db, ICapPublisher bus) { _db = db; _bus = bus; }

    [HttpGet]
    [Authorize(Policy = "HR_EMPLOYEE_READ")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status)
    {
        var q = _db.Employees.AsQueryable();
        if (!string.IsNullOrEmpty(search)) q = q.Where(e => e.FullName.Contains(search));
        if (!string.IsNullOrEmpty(status) && status!= "All") q = q.Where(e => e.Status == status);
        return Ok(await q.Include(e=>e.Branch).Include(e=>e.Department).ToListAsync());
    }

    [HttpPost]
    [Authorize(Policy = "HR_EMPLOYEE_WRITE")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        using var tx = _db.Database.BeginTransaction(_bus, autoCommit: false);
        var emp = new HR.Domain.Entities.Employee
        {
            ExternalUserId = dto.ExternalUserId?? Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            BranchId = dto.BranchId,
            DepartmentId = dto.DepartmentId,
            EmployeeNumber = $"ADM-HR-{DateTime.UtcNow:yyyyMM}-{await _db.Employees.CountAsync() + 1:D5}",
            Status = "Active",
            ContractType = dto.ContractType,
            HireDate = dto.HireDate
        };
        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();
        await _bus.PublishAsync("hr.employee.created", new EmployeeStatusChangedEvent
        {
            EmployeeId = emp.Id,
            ExternalUserId = emp.ExternalUserId?? emp.Id,
            NewStatus = "Active"
        });
        await tx.CommitAsync();
        return Ok(emp);
    }
}

public class CreateEmployeeDto
{
    public Guid? ExternalUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? ContractType { get; set; }
    public DateOnly? HireDate { get; set; }
}
