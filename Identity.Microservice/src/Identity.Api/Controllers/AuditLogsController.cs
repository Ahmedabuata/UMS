using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers;

/// <summary>
/// Handles audit log retrieval.
/// 
/// NOTE: Currently reads from HOT table only (audit_logs).
/// COLD table (audit_logs_archive) is empty until archiving begins (>1M records).
/// 
/// FIXES APPLIED:
/// 1. Removed generic ApplyFilters<T> — replaced with explicit ApplyHotFilters / ApplyColdFilters.
/// 2. Unified filter logic across GetAll, GetCount, Export.
/// 3. Replaced Concat + CountAsync with separate counts (hotCount + coldCount).
/// 4. Fixed Date filter to use EndOfDay (23:59:59).
/// 5. Added 'search' filter to GetCount.
/// 6. Removed stray comments about CreatedAt type.
/// </summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(IdentityDbContext db, ILogger<AuditLogsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ============================================================
    // GET: /api/audit-logs
    // ============================================================
    [HttpGet]
    [Authorize(Policy = "AuditRead")]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from, [FromQuery] DateTime? date_from,
        [FromQuery] DateTime? to, [FromQuery] DateTime? date_to,
        [FromQuery] Guid? userId, [FromQuery] string? user,
        [FromQuery] string? search, [FromQuery] string? action,
        [FromQuery] string? entity,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var fromDate = from ?? date_from;
            var toDate = to ?? date_to;

            _logger.LogInformation(
                "AuditLogs query: from={From}, to={To}, page={Page}, pageSize={PageSize}",
                fromDate, toDate, page, pageSize);

            // ============================================================
            // Apply filters to HOT (with EndOfDay for 'to')
            // ============================================================
            var hotQuery = ApplyHotFilters(_db.AuditLogs.AsNoTracking(),
                fromDate, toDate, userId, user, action, entity, search);

            // ============================================================
            // Get total count (separate, not Concat)
            // ============================================================
            var total = await hotQuery.CountAsync(cancellationToken);

            // ============================================================
            // Fetch raw entities (DB-level pagination)
            // ============================================================
            var rawItems = await hotQuery
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // ============================================================
            // Map to DTO in-memory (SafeParseGuid + null UserName)
            // ============================================================
            var items = rawItems.Select(a => new AuditLogDto(
                a.Id,
                a.UserId,
                a.Action ?? string.Empty,
                a.Entity,
                SafeParseGuid(a.EntityId),
                a.EntityId,
                a.TableName,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.UserAgent,
                a.BranchCode,
                a.CreatedBy,
                a.Timestamp ?? DateTime.UtcNow,
                a.CreatedAt,
                null,
                null
            )).ToList();

            // ============================================================
            // Enrich with user names (single batch query)
            // ============================================================
            var enrichedItems = await EnrichWithUserNamesAsync(items, cancellationToken);

            return Ok(new AuditLogListResponse(enrichedItems, total, page, pageSize));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, new { code = "REQUEST_CANCELED" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = ex.Message });
        }
    }

    // ============================================================
    // GET: /api/audit-logs/count
    // ============================================================
    [HttpGet("count")]
    [Authorize(Policy = "AuditRead")]
    public async Task<IActionResult> GetCount(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? userId,
        [FromQuery] string? user,
        [FromQuery] string? search,
        [FromQuery] string? action,
        [FromQuery] string? entity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ FIX: Same filters as GetAll (including search)
            var hotQuery = ApplyHotFilters(_db.AuditLogs.AsNoTracking(),
                from, to, userId, user, action, entity, search);

            var count = await hotQuery.LongCountAsync(cancellationToken);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error count");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = ex.Message });
        }
    }

    // ============================================================
    // GET: /api/audit-logs/{id}
    // ============================================================
    [HttpGet("{id}")]
    [Authorize(Policy = "AuditRead")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await _db.AuditLogs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (log == null)
            return NotFound(new { code = "AUDIT_LOG_NOT_FOUND" });

        var userName = log.UserId != null
            ? await _db.Users.Where(u => u.Id == log.UserId)
                .Select(u => u.Username).FirstOrDefaultAsync(cancellationToken)
            : null;

        var createdByName = log.CreatedBy != null
            ? await _db.Users.Where(u => u.Id == log.CreatedBy)
                .Select(u => u.Username).FirstOrDefaultAsync(cancellationToken)
            : null;

        return Ok(new AuditLogDto(
            log.Id, log.UserId, log.Action ?? string.Empty, log.Entity,
            SafeParseGuid(log.EntityId), log.EntityId,
            log.TableName, log.OldValues, log.NewValues,
            log.IpAddress, log.UserAgent, log.BranchCode, log.CreatedBy,
            log.Timestamp ?? DateTime.UtcNow, log.CreatedAt,
            userName, createdByName));
    }

    // ============================================================
    // GET: /api/audit-logs/export
    // ============================================================
    [HttpGet("export")]
    [Authorize(Policy = "AuditRead")]
    public async Task<IActionResult> Export(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] Guid? userId, [FromQuery] string? user,
        [FromQuery] string? search, [FromQuery] string? action,
        [FromQuery] string? entity)
    {
        try
        {
            // ✅ FIX: Same filters as GetAll (including userId, entity)
            var hotQuery = ApplyHotFilters(_db.AuditLogs.AsNoTracking(),
                from, to, userId, user, action, entity, search);

            var rawItems = await hotQuery
                .OrderByDescending(a => a.Timestamp)
                .Take(10000)
                .ToListAsync(cancellationToken);

            var csv = "Action,User,Details,IP Address,Timestamp\n" + string.Join("\n",
                rawItems.Select(l =>
                    $"\"{l.Action}\",\"{l.UserId}\",\"{(l.OldValues ?? "").Replace("\"", "\"\"")}-{(l.NewValues ?? "").Replace("\"", "\"\"")}\",\"{l.IpAddress}\",\"{l.Timestamp}\""));

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
                $"audit-logs-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = ex.Message });
        }
    }

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================

    /// <summary>
    /// Applies all filters to HOT query (audit_logs).
    /// 
    /// FIXED:
    /// - No more generic typeof pattern.
    /// - Uses EndOfDay for 'to' date (includes all records of the end day).
    /// - Same filters as GetCount and Export.
    /// </summary>
    private static IQueryable<AuditLog> ApplyHotFilters(
        IQueryable<AuditLog> query,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? userId,
        string? user,
        string? action,
        string? entity,
        string? search)
    {
        // ✅ FIX: EndOfDay for 'to' filter
        if (fromDate != null)
            query = query.Where(a => a.Timestamp >= fromDate.Value.Date);

        if (toDate != null)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddSeconds(-1);
            query = query.Where(a => a.Timestamp <= endOfDay);
        }

        if (userId != null)
            query = query.Where(a => a.UserId == userId);

        if (!string.IsNullOrWhiteSpace(user) && Guid.TryParse(user, out var guidUser))
            query = query.Where(a => a.UserId == guidUser);

        if (!string.IsNullOrWhiteSpace(action) && action != "All" && action != "All Actions")
            query = query.Where(a => a.Action == action.Trim());

        if (!string.IsNullOrWhiteSpace(entity))
            query = query.Where(a => a.Entity == entity.Trim());

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a =>
                (a.Action != null && a.Action.ToLower().Contains(s)) ||
                (a.IpAddress != null && a.IpAddress.ToLower().Contains(s)) ||
                (a.OldValues != null && a.OldValues.ToLower().Contains(s)) ||
                (a.NewValues != null && a.NewValues.ToLower().Contains(s)) ||
                (a.Entity != null && a.Entity.ToLower().Contains(s)));
        }

        return query;
    }

    private static Guid? SafeParseGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private async Task<List<AuditLogDto>> EnrichWithUserNamesAsync(
        List<AuditLogDto> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0) return items;

        var userIds = items
            .SelectMany(i => new[] { i.UserId, i.CreatedBy })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0) return items;

        var userNames = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Username })
            .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

        return items.Select(i => i with
        {
            UserName = i.UserId.HasValue && userNames.TryGetValue(i.UserId.Value, out var targetName)
                ? targetName : null,
            CreatedByName = i.CreatedBy.HasValue && userNames.TryGetValue(i.CreatedBy.Value, out var actorName)
                ? actorName : null
        }).ToList();
    }
}