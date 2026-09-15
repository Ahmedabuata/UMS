using System.Text.Json;
using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Identity.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Api.Services;

/// <summary>
/// Implements audit log management operations.
/// 
/// IMPORTANT: This service uses the HOT table only (audit_logs).
/// For unified HOT+COLD queries, use AuditLogsController.
/// </summary>
public class AuditService : IAuditService
{
    private readonly IdentityDbContext _db;
    private readonly ILogger<AuditService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditService(
        IdentityDbContext db,
        ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Helper method to safely parse string to Guid?
    /// </summary>
    private static Guid? ParseGuid(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        return Guid.TryParse(value, out var result) ? result : null;
    }

    /// <inheritdoc />
    public async Task LogAsync(
        string action,
        Guid? userId = null,
        string? entityId = null,
        string? entity = null,
        string? tableName = null,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? branchCode = null,
        Guid? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                UserId = userId,
                EntityId = entityId,
                Entity = entity,
                TableName = tableName,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues, _jsonOptions) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues, _jsonOptions) : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                BranchCode = branchCode,
                CreatedBy = createdBy ?? userId,
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.AuditLogs.Add(auditLog);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action}", action);
        }
    }

    /// <inheritdoc />
    public async Task<AuditLogListResponse> GetLogsAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var dbQuery = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            dbQuery = dbQuery.Where(l => l.Action != null && l.Action.Contains(action));
        }

        if (query.UserId.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Entity))
        {
            var entity = query.Entity.Trim();
            dbQuery = dbQuery.Where(l => l.Entity != null && l.Entity.Contains(entity));
        }

        if (query.From.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.Timestamp >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.Timestamp <= query.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.BranchCode))
        {
            var branchCode = query.BranchCode.Trim();
            dbQuery = dbQuery.Where(l => l.BranchCode == branchCode);
        }

        var total = await dbQuery.CountAsync(cancellationToken);

        // Fetch data first (DB-level pagination), then map in-memory
        var rawLogs = await dbQuery
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs in-memory (with all required fields)
        var logs = rawLogs.Select(l => new AuditLogDto(
            l.Id,
            l.UserId,
            l.Action ?? string.Empty,
            l.Entity,
            ParseGuid(l.EntityId),     // EntityId (computed in-memory)
            l.EntityId,                //  EntityIdString (raw from DB)
            l.TableName,
            l.OldValues,
            l.NewValues,
            l.IpAddress,
            l.UserAgent,
            l.BranchCode,
            l.CreatedBy,
            l.Timestamp ?? DateTime.UtcNow,
            l.CreatedAt,
            null,                      //  UserName (not populated here)
            null                       //  CreatedByName (not populated here)
        )).ToList();

        return new AuditLogListResponse(logs, total, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<AuditLogDto?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var l = await _db.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (l == null)
            return null;

        return new AuditLogDto(
            l.Id,
            l.UserId,
            l.Action ?? string.Empty,
            l.Entity,
            ParseGuid(l.EntityId),     // EntityId (computed in-memory)
            l.EntityId,                //  EntityIdString (raw from DB)
            l.TableName,
            l.OldValues,
            l.NewValues,
            l.IpAddress,
            l.UserAgent,
            l.BranchCode,
            l.CreatedBy,
            l.Timestamp ?? DateTime.UtcNow,
            l.CreatedAt,
            null,                      //  UserName
            null                       //  CreatedByName
        );
    }

    /// <inheritdoc />
    public async Task<AuditLogListResponse> GetLogsByUserIdAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await GetLogsAsync(new AuditLogQuery { UserId = userId, Page = page, PageSize = pageSize }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuditLogListResponse> GetLogsByActionAsync(
        string action,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await GetLogsAsync(new AuditLogQuery { Action = action, Page = page, PageSize = pageSize }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuditLogListResponse> GetLogsByDateRangeAsync(
        DateTime from,
        DateTime to,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await GetLogsAsync(new AuditLogQuery { From = from, To = to, Page = page, PageSize = pageSize }, cancellationToken);
    }
}