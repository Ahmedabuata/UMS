using Microsoft.AspNetCore.Mvc;
using University.API.Attributes;
using University.Core.Interfaces.Services.Security;
using University.Shared.Common;

namespace University.API.Controllers;

[ApiController]
[Route("api/security/audit-logs")]
[HasPermission("SECURITY_AUDIT_READ")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? search = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entity = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _auditLogService.QueryAsync(search, action, entity, from, to, page, pageSize);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Log([FromBody] AuditLogRequestDto dto)
    {
        var result = await _auditLogService.LogAsync(dto.Action, dto.UserId, dto.Details);
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

public class AuditLogRequestDto
{
    public string Action { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string Details { get; set; } = string.Empty;
}
