using Microsoft.AspNetCore.Mvc;
using University.API.Attributes;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Semesters;

namespace University.API.Controllers;

[ApiController]
[Route("api/Semesters")]
[HasPermission("SEMESTER_READ")]
public class SemestersController : ControllerBase
{
    private readonly ISemesterService _service;

    public SemestersController(ISemesterService service)
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
    [HasPermission("SEMESTER_WRITE")]
    public async Task<IActionResult> Create([FromBody] CreateSemesterRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("SEMESTER_WRITE")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateSemesterRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/open")]
    [HasPermission("SEMESTER_WRITE")]
    public async Task<IActionResult> Open(Guid id)
    {
        var result = await _service.OpenAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/close")]
    [HasPermission("SEMESTER_WRITE")]
    public async Task<IActionResult> Close(Guid id)
    {
        var result = await _service.CloseAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/set-current")]
    [HasPermission("SEMESTER_WRITE")]
    public async Task<IActionResult> SetCurrent(Guid id)
    {
        var result = await _service.SetCurrentAsync(id);
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