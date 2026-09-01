using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Students;

namespace University.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _studentService.GetAllAsync();
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _studentService.GetStudentByIdAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequestDto dto)
    {
        var result = await _studentService.CreateStudentAsync(dto);
        return result.IsFailure ? ErrorResult(result) : CreatedAtAction(nameof(GetById), new { id = 0 }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentRequestDto dto)
    {
        var result = await _studentService.UpdateStudentAsync(id, dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}/gpa")]
    public async Task<IActionResult> GetGpa(Guid id)
    {
        var result = await _studentService.GetStudentGPAAsync(id);
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
