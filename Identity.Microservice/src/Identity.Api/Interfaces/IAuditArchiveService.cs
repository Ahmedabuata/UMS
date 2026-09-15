using Identity.Api.DTOs;

namespace Identity.Api.Interfaces;

/// <summary>
/// Service responsible for archiving old audit logs from HOT to COLD storage.
/// 
/// All operations are transactional and idempotent.
/// This interface defines the contract that must be implemented by
/// the concrete AuditArchiveService class.
/// 
/// Methods:
/// - ArchiveOldLogsAsync:   Automated archiving (older than X months)
/// - ArchiveRangeAsync:     Manual archiving (specific date range)
/// - GetJobsAsync:          List all archive jobs (paginated)
/// - GetJobByIdAsync:       Get details of a specific archive job
/// - GetArchiveStatsAsync:  Get statistics (HOT vs COLD counts)
/// </summary>
public interface IAuditArchiveService
{
    /// <summary>
    /// Archives all audit logs older than the specified number of months.
    /// 
    /// This is the PRIMARY method used by the Background Job (Hangfire).
    /// It's called automatically on a schedule (e.g., monthly).
    /// 
    /// The method:
    /// 1. Calculates the cutoff date (NOW - monthsOld)
    /// 2. Creates a new AuditArchiveJob (status = RUNNING)
    /// 3. Begins a transaction
    /// 4. Copies records to audit_logs_archive
    /// 5. Deletes records from audit_logs
    /// 6. Updates the job (status = COMPLETED)
    /// 7. Commits the transaction
    /// </summary>
    /// <param name="monthsOld">Number of months to keep in HOT storage (e.g., 3).</param>
    /// <param name="triggeredBy">User ID who triggered the archive (NULL for automated).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Result containing batch ID, record count, and status.</returns>
    Task<ArchiveResultDto> ArchiveOldLogsAsync(
        int monthsOld,
        Guid? triggeredBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives audit logs within a specific date range.
    /// 
    /// Used for MANUAL archiving from the Admin Panel.
    /// 
    /// Example scenarios:
    /// - Admin wants to archive a specific quarter (e.g., Q1 2024).
    /// - Admin wants to archive a specific month for compliance.
    /// </summary>
    /// <param name="fromDate">Start of the date range (inclusive).</param>
    /// <param name="toDate">End of the date range (inclusive).</param>
    /// <param name="triggeredBy">User ID who triggered the archive.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Result containing batch ID, record count, and status.</returns>
    Task<ArchiveResultDto> ArchiveRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? triggeredBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of archive jobs.
    /// 
    /// Used for the Admin Panel "Archive Jobs" page.
    /// Returns jobs ordered by StartedAt descending (latest first).
    /// 
    /// The returned ArchiveJobDto includes a computed Duration field
    /// (CompletedAt - StartedAt) for monitoring purposes.
    /// </summary>
    /// <param name="page">Page number (1-based). Default: 1.</param>
    /// <param name="pageSize">Number of items per page (max: 100). Default: 20.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Paginated list of archive jobs.</returns>
    Task<ArchiveJobsPageDto> GetJobsAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific archive job by its ID.
    /// 
    /// Used for the "Archive Job Details" view.
    /// Returns NULL if the job is not found.
    /// </summary>
    /// <param name="jobId">The unique identifier of the archive job.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The archive job details, or NULL if not found.</returns>
    Task<ArchiveJobDto?> GetJobByIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves statistics about the archive system.
    /// 
    /// Shows:
    /// - HOT records count (audit_logs)
    /// - COLD records count (audit_logs_archive)
    /// - Last archive date and status
    /// - Total number of archive jobs
    /// 
    /// Used for the Admin Dashboard overview.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Statistics about the archive system.</returns>
    Task<ArchiveStatsDto> GetArchiveStatsAsync(
        CancellationToken cancellationToken = default);
}