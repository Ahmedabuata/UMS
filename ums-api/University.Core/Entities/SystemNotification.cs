using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class SystemNotification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType NotificationType { get; set; } = NotificationType.INFO;
    public bool IsRead { get; set; } = false;

    public User? User { get; set; }
}
