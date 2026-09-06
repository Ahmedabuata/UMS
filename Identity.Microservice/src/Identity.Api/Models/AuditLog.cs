using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Api.Models;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("action")]
    public string? Action { get; set; }

    [Column("entity")]
    public string? Entity { get; set; }

    [Column("entity_id")]
    public string? EntityId { get; set; }

    [Column("table_name")]
    public string? TableName { get; set; }

    [Column("old_values")]
    public string? OldValues { get; set; }

    [Column("new_values")]
    public string? NewValues { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("branch_code")]
    public string? BranchCode { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("timestamp")]
    public DateTime? Timestamp { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Compatibility for old code using EntityName
    [NotMapped]
    public string? EntityName { get => Entity; set => Entity = value; }
}
