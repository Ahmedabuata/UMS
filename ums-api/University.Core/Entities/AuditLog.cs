using University.Shared.Common;

namespace University.Core.Entities;

// SPECIAL - inherits AuditLogBase (NO UpdatedAt, NO IsActive - Rule 11)
public class AuditLog : AuditLogBase
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public string? EntityId { get; set; }
    public string? TableName { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public User? User { get; set; }
}
