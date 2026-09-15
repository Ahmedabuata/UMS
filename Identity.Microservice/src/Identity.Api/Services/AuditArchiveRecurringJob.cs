using Identity.Api.Interfaces;

namespace Identity.Api.Services;

/// <summary>
/// Recurring job for archiving old audit logs.
/// 
/// This class is executed automatically by Hangfire on a schedule
/// (e.g., monthly on the 1st day at 2:00 AM).
/// 
/// The job calls IAuditArchiveService.ArchiveOldLogsAsync() with
/// a default threshold of 3 months.
/// 
/// Design Decisions:
/// - The job does NOT contain business logic; it delegates to the Service.
/// - It catches exceptions to prevent Hangfire from marking the job as failed
///   (which would disable future executions).
/// - All execution details are logged for monitoring.
/// </summary>
public class AuditArchiveRecurringJob
{
    private readonly IAuditArchiveService _archiveService;
    private readonly ILogger<AuditArchiveRecurringJob> _logger;

    /// <summary>
    /// Default retention threshold in months.
    /// Logs older than this are archived to COLD storage.
    /// </summary>
    private const int DefaultMonthsOld = 3;

    public AuditArchiveRecurringJob(
        IAuditArchiveService archiveService,
        ILogger<AuditArchiveRecurringJob> logger)
    {
        _archiveService = archiveService;
        _logger = logger;
    }

    /// <summary>
    /// Main entry point for the recurring job.
    /// Called by Hangfire automatically (e.g., monthly).
    /// 
    /// Uses the default retention threshold (3 months).
    /// TriggeredBy = null (indicates automated execution).
    /// </summary>
    public async Task RunAsync()
    {
        await RunInternalAsync(DefaultMonthsOld, triggeredBy: null);
    }

    /// <summary>
    /// Alternative entry point for MANUAL archiving with a custom threshold.
    /// Used for testing or manual admin operations.
    /// 
    /// Example:
    /// - RunWithCustomMonthsAsync(6) → archives logs older than 6 months.
    /// </summary>
    /// <param name="monthsOld">Number of months to keep in HOT storage.</param>
    /// <param name="triggeredBy">User ID who triggered the archive (NULL for automated).</param>
    public async Task RunWithCustomMonthsAsync(int monthsOld, Guid? triggeredBy = null)
    {
        if (monthsOld <= 0)
        {
            _logger.LogWarning(
                "Invalid monthsOld value: {MonthsOld}. Using default: {Default}.",
                monthsOld, DefaultMonthsOld);

            monthsOld = DefaultMonthsOld;
        }

        await RunInternalAsync(monthsOld, triggeredBy);
    }

    /// <summary>
    /// Internal method that executes the archive with full error handling.
    /// 
    /// Catches all exceptions to prevent Hangfire from marking the job as
    /// permanently failed (which would stop future executions).
    /// </summary>
    private async Task RunInternalAsync(int monthsOld, Guid? triggeredBy)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "=== Audit Archive Recurring Job Started === " +
            "MonthsOld: {MonthsOld}, TriggeredBy: {TriggeredBy}, Time: {Time}",
            monthsOld,
            triggeredBy?.ToString() ?? "AUTOMATED",
            startTime.ToString("yyyy-MM-dd HH:mm:ss UTC"));

        try
        {
            var result = await _archiveService.ArchiveOldLogsAsync(
                monthsOld: monthsOld,
                triggeredBy: triggeredBy);

            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "=== Audit Archive Recurring Job Completed === " +
                "JobId: {JobId}, BatchId: {BatchId}, " +
                "RecordsArchived: {RecordsArchived}, " +
                "Status: {Status}, Duration: {Duration:mm\\:ss}",
                result.JobId,
                result.BatchId,
                result.RecordsArchived,
                result.Status,
                duration);
        }
        catch (OperationCanceledException)
        {
            // Don't log as error - it's a normal cancellation (app shutdown).
            _logger.LogWarning(
                "Audit Archive Recurring Job was canceled (app shutting down?).");
        }
        catch (Exception ex)
        {
            // Log the error but DO NOT re-throw.
            // Re-throwing would cause Hangfire to mark the job as Failed
            // and might disable future executions.
            _logger.LogError(ex,
                "Audit Archive Recurring Job FAILED. " +
                "Exception: {ExceptionType}, Message: {Message}",
                ex.GetType().Name,
                ex.Message);

            // The job will be retried by Hangfire based on its retry policy.
            // However, we don't re-throw to keep the schedule intact.
        }
    }
}