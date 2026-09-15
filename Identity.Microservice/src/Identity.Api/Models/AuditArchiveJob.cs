using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

/// <summary>
/// Represents the status of an archive job.
/// Using an enum to prevent typos and ensure consistency.
/// 
/// IMPORTANT: EF Core will save this enum as a STRING (not int) 
/// via HasConversion<string>() configured in AuditArchiveJobConfiguration.
/// This matches the 'varchar(50)' column type in PostgreSQL.
/// </summary>
public enum ArchiveJobStatus
{
    /// <summary>
    /// The archive job is currently running.
    /// </summary>
    RUNNING,

    /// <summary>
    /// The archive job completed successfully.
    /// </summary>
    COMPLETED,

    /// <summary>
    /// The archive job failed with an error.
    /// </summary>
    FAILED
}

/// <summary>
/// Represents an archive job execution (batch).
/// Tracks each time the archive process runs, its status, and metadata.
/// 
/// Design Decisions:
/// - Id and BatchId are generated in C# (no DEFAULT in Postgres).
/// - Status is stored as STRING in DB (via HasConversion<string>()).
/// - TriggeredBy is NULL for automated jobs, populated for manual runs.
/// - No Foreign Keys (by design) for performance and deletion safety.
/// </summary>
[Table("audit_archive_jobs")]
public class AuditArchiveJob
{
    /// <summary>
    /// Gets or sets the unique identifier for this job execution.
    /// Generated in C# (Postgres has no DEFAULT for this column).
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the unique batch identifier.
    /// Used to link archived records to this job.
    /// Generated in C# (Postgres has no DEFAULT for this column).
    /// 
    /// IMPORTANT: This field has a UNIQUE constraint in the database.
    /// The initializer (= Guid.NewGuid()) is a safety net; the Service 
    /// MUST explicitly set it before insertion to avoid duplicate defaults.
    /// </summary>
    [Column("batch_id")]
    public Guid BatchId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the status of the job.
    /// Stored as STRING in DB (RUNNING, COMPLETED, FAILED).
    /// 
    /// IMPORTANT: The HasConversion<string>() configuration in 
    /// AuditArchiveJobConfiguration ensures EF Core saves this as text, not int.
    /// Without it, EF Core would save 0/1/2 and PostgreSQL would reject 
    /// the insert (column type mismatch).
    /// </summary>
    [Column("status")]
    [MaxLength(50)]
    public ArchiveJobStatus Status { get; set; } = ArchiveJobStatus.RUNNING;

    /// <summary>
    /// Gets or sets the number of records archived in this job.
    /// </summary>
    [Column("records_count")]
    public int RecordsCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the encryption key version used for this batch.
    /// Supports future key rotation.
    /// NULL when encryption is not applied (current state).
    /// </summary>
    [Column("encryption_key_version")]
    public int? EncryptionKeyVersion { get; set; }

    /// <summary>
    /// Gets or sets the error message if the job failed.
    /// NULL when the job succeeds.
    /// </summary>
    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the job started.
    /// Defaults to UTC now when the entity is created.
    /// </summary>
    [Column("started_at")]
    public DateTime? StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when the job completed.
    /// NULL while the job is still running.
    /// </summary>
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who triggered the archive.
    /// NULL for automated (background) jobs.
    /// Populated with the Admin's UserId for manual runs.
    /// 
    /// No FK constraint (by design) to avoid:
    /// - Deletion issues if the user is removed.
    /// - Performance overhead on large archive tables.
    /// </summary>
    [Column("triggered_by")]
    public Guid? TriggeredBy { get; set; }
}