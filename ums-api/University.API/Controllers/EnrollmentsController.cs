using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Enrollments;

namespace University.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequestDto dto)
    {
        var result = await _enrollmentService.EnrollAsync(dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost("{id:guid}/drop")]
    public async Task<IActionResult> Drop(Guid id, [FromBody] DropCourseRequestDto? dto)
    {
        var reason = dto?.Reason;
        var result = await _enrollmentService.DropAsync(id, reason);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("student/{studentId:guid}/semester/{semesterId:guid}")]
    public async Task<IActionResult> GetForStudent(Guid studentId, Guid semesterId)
    {
        var result = await _enrollmentService.GetStudentEnrollmentsAsync(studentId, semesterId);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("student/{studentId:guid}/course/{courseId:guid}/check-prerequisites")]
    public async Task<IActionResult> CheckPrerequisites(Guid studentId, Guid courseId)
    {
        var result = await _enrollmentService.CheckPrerequisitesAsync(studentId, courseId);
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
