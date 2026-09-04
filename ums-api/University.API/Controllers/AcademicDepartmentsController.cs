using Microsoft.AspNetCore.Mvc;
using University.API.Attributes;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.AcademicDepartments;

namespace University.API.Controllers;

[ApiController]
[Route("api/AcademicDepartments")]
[HasPermission("ACADEMIC_DEPT_READ")]
public class AcademicDepartmentsController : ControllerBase
{
    private readonly IAcademicDepartmentService _service;

    public AcademicDepartmentsController(IAcademicDepartmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? facultyId)
    {
        var result = await _service.GetAllAsync(facultyId);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost]
    [HasPermission("ACADEMIC_DEPT_WRITE")]
    public async Task<IActionResult> Create([FromBody] CreateAcademicDepartmentRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("ACADEMIC_DEPT_WRITE")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateAcademicDepartmentRequestDto dto)
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