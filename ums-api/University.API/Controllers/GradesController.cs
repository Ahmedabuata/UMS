using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Grades;

namespace University.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GradesController : ControllerBase
{
    private readonly IGradeService _gradeService;

    public GradesController(IGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitGradeRequestDto dto)
    {
        var result = await _gradeService.SubmitGradeAsync(dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGradeRequestDto dto)
    {
        var result = await _gradeService.UpdateGradeAsync(id, dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost("{id:guid}/lock")]
    public async Task<IActionResult> Lock(Guid id)
    {
        var result = await _gradeService.LockGradeAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<IActionResult> GetForStudent(Guid studentId)
    {
        var result = await _gradeService.GetStudentGradesAsync(studentId);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("student/{studentId:guid}/gpa")]
    public async Task<IActionResult> CalculateGpa(Guid studentId)
    {
        var result = await _gradeService.CalculateGPAAsync(studentId);
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
