namespace HR.Shared.Contracts.Events;
public class UserCreatedEvent
{
    public Guid ExternalUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class UserDeletedEvent
{
    public Guid ExternalUserId { get; set; }
}
public class EmployeeStatusChangedEvent
{
    public Guid EmployeeId { get; set; }
    public Guid ExternalUserId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
}
