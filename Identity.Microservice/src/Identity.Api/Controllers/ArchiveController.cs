using System.Security.Claims;
using Identity.Api.DTOs;
using Identity.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Identity.Api.Controllers;

/// <summary>
/// Handles archive management operations (Cold Storage).
/// 
/// This controller provides:
/// - Listing archive jobs (with pagination).
/// - Viewing job details.
/// - Viewing archive statistics (HOT vs COLD).
/// - Triggering manual archiving.
/// 
/// All endpoints require the "AuditArchiveManage" permission.
/// </summary>
[ApiController]
[Route("api/archive")]
[Authorize]
[EnableRateLimiting("api")]
public class ArchiveController : ControllerBase
{
    private readonly IAuditArchiveService _archiveService;
    private readonly ILogger<ArchiveController> _logger;

    /// <summary>
    /// Minimum allowed value for monthsOld parameter.
    /// </summary>
    private const int MinMonthsOld = 1;

    /// <summary>
    /// Maximum allowed value for monthsOld parameter.
    /// Prevents archiving too much data accidentally.
    /// </summary>
    private const int MaxMonthsOld = 24;

    /// <summary>
    /// Maximum allowed page size (prevents DoS attacks).
    /// </summary>
    private const int MaxPageSize = 100;

    public ArchiveController(
        IAuditArchiveService archiveService,
        ILogger<ArchiveController> logger)
    {
        _archiveService = archiveService;
        _logger = logger;
    }

    // ============================================================
    // GET: /api/archive/jobs
    // ============================================================
    /// <summary>
    /// Retrieves a paginated list of archive jobs.
    /// 
    /// Query Parameters:
    /// - page: Page number (1-based, clamped to >= 1). Default: 1.
    /// - pageSize: Number of items per page (clamped to 1-100). Default: 20.
    /// 
    /// FIX: Added Clamp to prevent negative/oversized pagination.
    /// </summary>
    [HttpGet("jobs")]
    [Authorize(Policy = "AuditArchiveManage")]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            //  FIX: Clamp pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var result = await _archiveService.GetJobsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The archive jobs request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving archive jobs.");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred." });
        }
    }

    // ============================================================
    // GET: /api/archive/jobs/{id}
    // ============================================================
    /// <summary>
    /// Retrieves a specific archive job by its ID.
    /// </summary>
    [HttpGet("jobs/{id}")]
    [Authorize(Policy = "AuditArchiveManage")]
    public async Task<IActionResult> GetJobById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var job = await _archiveService.GetJobByIdAsync(id, cancellationToken);

            if (job == null)
                return NotFound(new { code = "ARCHIVE_JOB_NOT_FOUND", message = "Archive job not found." });

            return Ok(job);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The archive job request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving archive job with ID {JobId}.", id);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred." });
        }
    }

    // ============================================================
    // GET: /api/archive/stats
    // ============================================================
    /// <summary>
    /// Retrieves archive statistics.
    /// 
    /// Returns:
    /// - HOT records count (audit_logs)
    /// - COLD records count (audit_logs_archive)
    /// - Last archive date and status
    /// - Total number of archive jobs
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Policy = "AuditArchiveManage")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _archiveService.GetArchiveStatsAsync(cancellationToken);
            return Ok(stats);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The archive stats request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving archive stats.");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred." });
        }
    }

    // ============================================================
    // POST: /api/archive/run
    // ============================================================
    /// <summary>
    /// Triggers a MANUAL archive operation.
    /// 
    /// Query Parameters:
    /// - monthsOld: Number of months to keep in HOT (1-24). Default: 3.
    /// 
    /// The TriggeredBy field is automatically populated from the JWT.
    /// 
    /// Example:
    /// POST /api/archive/run?monthsOld=6
    /// → Archives logs older than 6 months.
    /// 
    /// FIX: Improved userId extraction to support multiple JWT claim types.
    /// </summary>
    [HttpPost("run")]
    [Authorize(Policy = "AuditArchiveManage")]
    public async Task<IActionResult> RunManualArchive(
        [FromQuery] int monthsOld = 3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate monthsOld
            if (monthsOld < MinMonthsOld || monthsOld > MaxMonthsOld)
            {
                return BadRequest(new
                {
                    code = "INVALID_MONTHS_OLD",
                    message = $"monthsOld must be between {MinMonthsOld} and {MaxMonthsOld}."
                });
            }

            // ============================================================
            //  FIX: Extract UserId from JWT with Fallback Chain
            // ============================================================
            // ASP.NET Core may map "sub" to ClaimTypes.NameIdentifier.
            // We try multiple claim types to support any JWT format.
            // ============================================================
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User?.FindFirst("sub")?.Value
                          ?? User?.FindFirst("userId")?.Value
                          ?? User?.FindFirst("uid")?.Value;

            Guid? triggeredBy = null;

            if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                triggeredBy = parsedUserId;
            }
            else
            {
                // Log all claims for debugging if userId cannot be parsed
                _logger.LogWarning(
                    "Could not parse triggeredBy from claims. Available claims: {Claims}",
                    User?.Claims != null
                        ? string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))
                        : "No claims");
            }

            _logger.LogInformation(
                "Manual archive triggered by user {UserId} with monthsOld={MonthsOld}.",
                triggeredBy?.ToString() ?? "UNKNOWN",
                monthsOld);

            var result = await _archiveService.ArchiveOldLogsAsync(
                monthsOld: monthsOld,
                triggeredBy: triggeredBy,
                cancellationToken: cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            // Invalid monthsOld (from service validation)
            _logger.LogWarning(ex, "Invalid monthsOld argument.");
            return BadRequest(new { code = "INVALID_ARGUMENT", message = ex.Message });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The manual archive request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running manual archive.");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred." });
        }
    }
}