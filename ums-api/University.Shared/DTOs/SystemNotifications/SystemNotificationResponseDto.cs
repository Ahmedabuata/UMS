using University.Shared.Enums;

namespace University.Shared.DTOs.SystemNotifications;

public class SystemNotificationResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType NotificationType { get; set; }
    public bool IsRead { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
