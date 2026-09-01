namespace University.Shared.Common;

// SPECAIL - audit_logs only - no UpdatedAt, no IsActive (Rule 11)
public abstract class AuditLogBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
