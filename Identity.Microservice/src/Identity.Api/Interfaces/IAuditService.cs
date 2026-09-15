using Identity.Api.DTOs;

namespace Identity.Api.Interfaces;

/// <summary>
/// Defines the contract for audit log management operations.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an audit entry with the specified details.
    /// </summary>
    Task LogAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of audit logs with optional filtering.
    /// </summary>
    Task<AuditLogListResponse> GetLogsAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific audit log by its unique identifier.
    /// </summary>
    Task<AuditLogDto?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit logs for a specific user.
    /// </summary>
    Task<AuditLogListResponse> GetLogsByUserIdAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit logs for a specific action.
    /// </summary>
    Task<AuditLogListResponse> GetLogsByActionAsync(
        string action,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit logs within a date range.
    /// </summary>
    Task<AuditLogListResponse> GetLogsByDateRangeAsync(
        DateTime from,
        DateTime to,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}