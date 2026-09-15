namespace Identity.Api.DTOs;

/// <summary>
/// Represents the result of an archive operation.
/// Returned by the Archive Service after a successful or failed archive.
/// 
/// This DTO is used as the return value of:
/// - ArchiveOldLogsAsync()
/// - ArchiveRangeAsync()
/// </summary>
public record ArchiveResultDto(
    Guid JobId,
    Guid BatchId,
    string Status,
    int RecordsArchived,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage = null
);

/// <summary>
/// DTO for displaying an archive job in the Admin Panel.
/// Used for the "Archive Jobs" list view.
/// 
/// NOTE: The Duration field is COMPUTED in the Service (not stored in DB)
/// to simplify the Frontend display and monitoring dashboard.
/// 
/// Formula: Duration = CompletedAt - StartedAt
/// - If the job is COMPLETED → Duration has a value (e.g., 00:03:45)
/// - If the job is RUNNING → Duration is null
/// </summary>
public record ArchiveJobDto(
    Guid Id,
    Guid BatchId,
    string Status,
    int RecordsCount,
    int? EncryptionKeyVersion,
    string? ErrorMessage,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    Guid? TriggeredBy,
    string? TriggeredByName = null,     // Populated via join with Users table
    TimeSpan? Duration = null           // Computed: CompletedAt - StartedAt
);

/// <summary>
/// Paginated response for the archive jobs list.
/// Used for the "Archive Jobs" page with pagination.
/// 
/// This DTO is used as the return value of:
/// - GetJobsAsync()
/// </summary>
public record ArchiveJobsPageDto(
    List<ArchiveJobDto> Items,
    int Total,
    int Page,
    int PageSize
);

/// <summary>
/// Statistics about the archive system.
/// Shows HOT vs COLD counts and last archive info.
/// 
/// This DTO is used as the return value of:
/// - GetArchiveStatsAsync()
/// </summary>
public record ArchiveStatsDto(
    long HotRecordsCount,
    long ColdRecordsCount,
    DateTime? LastArchiveDate,
    Guid? LastArchiveBatchId,
    string? LastArchiveStatus,
    long TotalArchiveJobs
);