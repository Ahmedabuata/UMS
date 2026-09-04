using Microsoft.AspNetCore.Mvc;
using University.API.Attributes;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.AdministrativeDepartments;

namespace University.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[HasPermission("ADMIN_DEPT_READ")]
public class AdministrativeDepartmentsController : ControllerBase
{
    private readonly IAdministrativeDepartmentService _service;

    public AdministrativeDepartmentsController(IAdministrativeDepartmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost]
    [HasPermission("ADMIN_DEPT_WRITE")]
    public async Task<IActionResult> Create([FromBody] CreateAdministrativeDepartmentRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("ADMIN_DEPT_WRITE")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdministrativeDepartmentRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [SuperAdminOnly]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/restore")]
    [HasPermission("ADMIN_DEPT_WRITE")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
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
