using System.Diagnostics;
using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Interfaces;
using Identity.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Services;

/// <summary>
/// Implementation of IAuditArchiveService.
/// 
/// Handles the archiving of old audit logs with full transactional safety.
/// All operations are idempotent and logged in audit_archive_jobs.
/// 
/// Core Logic:
/// - ExecuteArchiveAsync: The heart of the archiving process.
///   Uses ExecuteSqlInterpolatedAsync for bulk operations (SAFE from SQL Injection).
///   Wrapped in a transaction to ensure atomicity.
/// 
/// Idempotency:
/// - Each archive run generates a unique BatchId.
/// - The database has a UNIQUE constraint on batch_id.
/// - If the same batch_id is attempted twice, the second attempt fails safely.
/// 
/// Security Rule (Golden Rule):
/// - Raw + $"..."          =  DANGEROUS (SQL Injection)
/// - Raw + Parameters      = SAFE (but order-dependent)
/// - Interpolated + $"..." =  SAFE and CLEAN (BEST)
/// </summary>
public class AuditArchiveService : IAuditArchiveService
{
    private readonly IdentityDbContext _db;
    private readonly ILogger<AuditArchiveService> _logger;

    public AuditArchiveService(
        IdentityDbContext db,
        ILogger<AuditArchiveService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ============================================================
    // PUBLIC METHODS
    // ============================================================

    /// <inheritdoc />
    public async Task<ArchiveResultDto> ArchiveOldLogsAsync(
        int monthsOld,
        Guid? triggeredBy = null,
        CancellationToken cancellationToken = default)
    {
        if (monthsOld <= 0)
            throw new ArgumentException("monthsOld must be greater than 0.", nameof(monthsOld));

        var cutoffDate = DateTime.UtcNow.AddMonths(-monthsOld);

        _logger.LogInformation(
            "Starting archive process for logs older than {CutoffDate} ({MonthsOld} months).",
            cutoffDate, monthsOld);

        return await ExecuteArchiveAsync(
            fromDate: DateTime.MinValue,
            toDate: cutoffDate,
            triggeredBy: triggeredBy,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ArchiveResultDto> ArchiveRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? triggeredBy = null,
        CancellationToken cancellationToken = default)
    {
        if (fromDate >= toDate)
            throw new ArgumentException("fromDate must be earlier than toDate.");

        _logger.LogInformation(
            "Starting manual archive for range [{FromDate} - {ToDate}].",
            fromDate, toDate);

        return await ExecuteArchiveAsync(
            fromDate: fromDate,
            toDate: toDate,
            triggeredBy: triggeredBy,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ArchiveJobsPageDto> GetJobsAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.AuditArchiveJobs
            .AsNoTracking()
            .OrderByDescending(j => j.StartedAt);

        var total = await query.CountAsync(cancellationToken);

        var jobs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new ArchiveJobDto(
                j.Id,
                j.BatchId,
                j.Status.ToString(),
                j.RecordsCount,
                j.EncryptionKeyVersion,
                j.ErrorMessage,
                j.StartedAt,
                j.CompletedAt,
                j.TriggeredBy,
                null, // TriggeredByName (populated below)
                j.CompletedAt.HasValue && j.StartedAt.HasValue
                    ? j.CompletedAt.Value - j.StartedAt.Value
                    : (TimeSpan?)null
            ))
            .ToListAsync(cancellationToken);

        // Populate TriggeredByName (no FK, so separate query)
        var userIds = jobs
            .Where(j => j.TriggeredBy.HasValue)
            .Select(j => j.TriggeredBy!.Value)
            .Distinct()
            .ToList();

        if (userIds.Any())
        {
            var userNames = await _db.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username })
                .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

            jobs = jobs.Select(j => j with
            {
                TriggeredByName = j.TriggeredBy.HasValue && userNames.ContainsKey(j.TriggeredBy.Value)
                    ? userNames[j.TriggeredBy.Value]
                    : null
            }).ToList();
        }

        return new ArchiveJobsPageDto(jobs, total, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<ArchiveJobDto?> GetJobByIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _db.AuditArchiveJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null) return null;

        string? triggeredByName = null;
        if (job.TriggeredBy.HasValue)
        {
            triggeredByName = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == job.TriggeredBy.Value)
                .Select(u => u.Username)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ArchiveJobDto(
            job.Id,
            job.BatchId,
            job.Status.ToString(),
            job.RecordsCount,
            job.EncryptionKeyVersion,
            job.ErrorMessage,
            job.StartedAt,
            job.CompletedAt,
            job.TriggeredBy,
            triggeredByName,
            job.CompletedAt.HasValue && job.StartedAt.HasValue
                ? job.CompletedAt.Value - job.StartedAt.Value
                : (TimeSpan?)null
        );
    }

    /// <inheritdoc />
    public async Task<ArchiveStatsDto> GetArchiveStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var hotCount = await _db.AuditLogs
            .LongCountAsync(cancellationToken);

        var coldCount = await _db.AuditLogsArchive
            .LongCountAsync(cancellationToken);

        var lastJob = await _db.AuditArchiveJobs
            .AsNoTracking()
            .OrderByDescending(j => j.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var totalJobs = await _db.AuditArchiveJobs
            .LongCountAsync(cancellationToken);

        return new ArchiveStatsDto(
            HotRecordsCount: hotCount,
            ColdRecordsCount: coldCount,
            LastArchiveDate: lastJob?.CompletedAt ?? lastJob?.StartedAt,
            LastArchiveBatchId: lastJob?.BatchId,
            LastArchiveStatus: lastJob?.Status.ToString(),
            TotalArchiveJobs: totalJobs
        );
    }

    // ============================================================
    // PRIVATE METHODS (Core Logic)
    // ============================================================

    /// <summary>
    /// Core archive execution logic with transaction and error handling.
    /// 
    /// Steps:
    /// 1. Create a new AuditArchiveJob (status = RUNNING)
    /// 2. Begin a transaction
    /// 3. INSERT INTO audit_logs_archive (SELECT from audit_logs)
    /// 4. DELETE FROM audit_logs (WHERE timestamp in range)
    /// 5. UPDATE AuditArchiveJob (status = COMPLETED)
    /// 6. COMMIT
    /// 
    /// On failure:
    /// - ROLLBACK transaction
    /// - UPDATE AuditArchiveJob (status = FAILED)
    /// 
    /// SECURITY: Uses ExecuteSqlInterpolatedAsync (parameterized, safe from SQL Injection).
    /// </summary>
    private async Task<ArchiveResultDto> ExecuteArchiveAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? triggeredBy,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var batchId = Guid.NewGuid();

        // ============================================================
        // Step 1: Create the job record first (status = RUNNING)
        // ============================================================
        var job = new AuditArchiveJob
        {
            Id = Guid.NewGuid(),
            BatchId = batchId,
            Status = ArchiveJobStatus.RUNNING,
            RecordsCount = 0,
            StartedAt = DateTime.UtcNow,
            TriggeredBy = triggeredBy
        };

        try
        {
            _db.AuditArchiveJobs.Add(job);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Archive job {JobId} (batch {BatchId}) created with status RUNNING.",
                job.Id, batchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create archive job record.");
            throw;
        }

        // ============================================================
        // Step 2: Execute the archive in a transaction
        // ============================================================
        await using var transaction = await _db.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            // ============================================================
            // 2.1. Insert into archive table
            // ============================================================
            // ✅ Using ExecuteSqlInterpolatedAsync (SAFE from SQL Injection)
            // Golden Rule: Interpolated + $"..." = Safe
            // ============================================================
            var archivedAt = DateTime.UtcNow;

            var insertedCount = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO audit_logs_archive 
                    (id, user_id, action, entity, entity_id, table_name, 
                     old_values, new_values, ip_address, user_agent, 
                     branch_code, created_by, timestamp, created_at, 
                     archived_at, archive_batch_id, archived_by)
                SELECT 
                    id, user_id, action, entity, entity_id, table_name, 
                    old_values, new_values, ip_address, user_agent, 
                    branch_code, created_by, timestamp, created_at, 
                    {archivedAt}, {batchId}, {triggeredBy}
                FROM audit_logs
                WHERE timestamp >= {fromDate} AND timestamp <= {toDate}",
                cancellationToken);

            _logger.LogInformation(
                "Inserted {Count} records into archive for batch {BatchId}.",
                insertedCount, batchId);

            // ============================================================
            // 2.2. Delete from live table
            // ============================================================
            // ✅ Using ExecuteSqlInterpolatedAsync (SAFE)
            // ============================================================
            var deletedCount = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                DELETE FROM audit_logs
                WHERE timestamp >= {fromDate} AND timestamp <= {toDate}",
                cancellationToken);

            _logger.LogInformation(
                "Deleted {Count} records from live table for batch {BatchId}.",
                deletedCount, batchId);

            // ============================================================
            // 2.3. Update the job record with success
            // ============================================================
            job.Status = ArchiveJobStatus.COMPLETED;
            job.RecordsCount = insertedCount;
            job.CompletedAt = DateTime.UtcNow;

            _db.AuditArchiveJobs.Update(job);
            await _db.SaveChangesAsync(cancellationToken);

            // ============================================================
            // 2.4. Commit transaction
            // ============================================================
            await transaction.CommitAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Archive batch {BatchId} completed successfully in {ElapsedMs}ms. Records: {Count}.",
                batchId, stopwatch.ElapsedMilliseconds, insertedCount);

            return new ArchiveResultDto(
                JobId: job.Id,
                BatchId: batchId,
                Status: job.Status.ToString(),
                RecordsArchived: insertedCount,
                StartedAt: job.StartedAt!.Value,
                CompletedAt: job.CompletedAt
            );
        }
        catch (Exception ex)
        {
            // Rollback the transaction
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex,
                "Archive batch {BatchId} failed. Transaction rolled back.",
                batchId);

            // Update job record with failure (in a separate transaction)
            await LogJobFailureAsync(job, ex, cancellationToken);

            stopwatch.Stop();
            _logger.LogError(
                "Archive batch {BatchId} failed after {ElapsedMs}ms.",
                batchId, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    /// <summary>
    /// Updates the job record with FAILED status and error message.
    /// This runs in a separate transaction since the main one was rolled back.
    /// </summary>
    private async Task LogJobFailureAsync(
        AuditArchiveJob job,
        Exception ex,
        CancellationToken cancellationToken)
    {
        try
        {
            job.Status = ArchiveJobStatus.FAILED;
            job.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
            job.CompletedAt = DateTime.UtcNow;

            _db.AuditArchiveJobs.Update(job);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Archive job {JobId} marked as FAILED in database.",
                job.Id);
        }
        catch (Exception logEx)
        {
            // If even logging fails, we can't do much. Just log it.
            _logger.LogCritical(logEx,
                "CRITICAL: Failed to update job {JobId} status to FAILED.",
                job.Id);
        }
    }
}