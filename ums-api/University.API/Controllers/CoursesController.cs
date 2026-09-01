using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Courses;

namespace University.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _courseService.GetAllAsync();
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _courseService.GetCourseByIdAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}/prerequisites")]
    public async Task<IActionResult> GetWithPrerequisites(Guid id)
    {
        var result = await _courseService.GetWithPrerequisitesAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequestDto dto)
    {
        var result = await _courseService.CreateCourseAsync(dto);
        return result.IsFailure ? ErrorResult(result) : CreatedAtAction(nameof(GetById), new { id = 0 }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseRequestDto dto)
    {
        var result = await _courseService.UpdateCourseAsync(id, dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _courseService.DeleteCourseAsync(id);
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
