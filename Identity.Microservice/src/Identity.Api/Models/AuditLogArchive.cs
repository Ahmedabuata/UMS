using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

/// <summary>
/// Represents an archived audit log record.
/// This entity maps to the 'audit_logs_archive' table (Cold Storage).
/// Read-only: no updates or deletes are expected on this entity.
/// </summary>
[Table("audit_logs_archive")]
public class AuditLogArchive
{
    /// <summary>
    /// Gets or sets the unique identifier (copied from the original audit_logs.id).
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the target user ID (the user affected by the action).
    /// </summary>
    [Column("user_id")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the action performed (e.g., ACTIVATE_USER, DELETE_ROLE).
    /// </summary>
    [Column("action")]
    [MaxLength(200)]
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the entity type (e.g., users, roles, groups).
    /// </summary>
    [Column("entity")]
    [MaxLength(200)]
    public string? Entity { get; set; }

    /// <summary>
    /// Gets or sets the entity identifier (the ID of the affected entity).
    /// Stored as string to support different entity types.
    /// </summary>
    [Column("entity_id")]
    [MaxLength(200)]
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the name of the affected table.
    /// </summary>
    [Column("table_name")]
    [MaxLength(200)]
    public string? TableName { get; set; }

    /// <summary>
    /// Gets or sets the old values (JSON) before the change.
    /// </summary>
    [Column("old_values")]
    public string? OldValues { get; set; }

    /// <summary>
    /// Gets or sets the new values (JSON) after the change.
    /// </summary>
    [Column("new_values")]
    public string? NewValues { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the actor.
    /// </summary>
    [Column("ip_address")]
    [MaxLength(100)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent of the actor's browser.
    /// </summary>
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the branch code associated with the action.
    /// </summary>
    [Column("branch_code")]
    [MaxLength(50)]
    public string? BranchCode { get; set; }

    /// <summary>
    /// Gets or sets the actor user ID (the user who performed the action).
    /// </summary>
    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the original timestamp of the event.
    /// </summary>
    [Column("timestamp")]
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the original creation timestamp.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this record was archived.
    /// </summary>
    [Column("archived_at")]
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    /// Gets or sets the batch ID linking this record to an archive job.
    /// No FK constraint (by design).
    /// </summary>
    [Column("archive_batch_id")]
    public Guid? ArchiveBatchId { get; set; }

    /// <summary>
    /// Gets or sets the user who triggered the archive (NULL for automated).
    /// No FK constraint (by design).
    /// </summary>
    [Column("archived_by")]
    public Guid? ArchivedBy { get; set; }
}