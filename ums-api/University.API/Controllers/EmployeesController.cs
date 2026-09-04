using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using University.API.Attributes;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Employees;

namespace University.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[HasPermission("HR_EMPLOYEE_READ")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeesController(IEmployeeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? department = null,
        [FromQuery] string? branch = null,
        [FromQuery] string? contractType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetAllAsync(department, branch, contractType, status, search);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("academic-titles")]
    public async Task<IActionResult> GetByAcademicTitle([FromQuery] string? academicTitle = null)
    {
        var result = await _service.GetByAcademicTitleAsync(academicTitle);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("number-preview")]
    public async Task<IActionResult> PreviewNumber([FromQuery] Guid departmentId)
    {
        var result = await _service.PreviewEmployeeNumberAsync(departmentId);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("without-user-account")]
    public async Task<IActionResult> GetWithoutUserAccounts()
    {
        var result = await _service.GetWithoutUserAccountsAsync();
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost]
    [HasPermission("HR_EMPLOYEE_WRITE")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("HR_EMPLOYEE_WRITE")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [SuperAdminOnly]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id, CurrentUserId());
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/deactivate")]
    [HasPermission("HR_EMPLOYEE_STATUS")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _service.DeactivateAsync(id, CurrentUserId());
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/activate")]
    [HasPermission("HR_EMPLOYEE_STATUS")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _service.ActivateAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/restore")]
    [HasPermission("HR_EMPLOYEE_STATUS")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    private Guid CurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private IActionResult ErrorResult<T>(Result<T> result)
    {
        if (result.Error is null)
        {
            return BadRequest();
        }
        return result.Error.Type switch
        {
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Unauthorized => Unauthorized(result.Error),
            ErrorType.Forbidden => Forbid(),
            _ => BadRequest(result.Error)
        };
    }
}