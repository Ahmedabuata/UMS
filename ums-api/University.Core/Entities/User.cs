using University.Shared.Common;

namespace University.Core.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Guid? BranchId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime? LastLogin { get; set; }

    public Branch? Branch { get; set; }
    public Role? Role { get; set; }
    public Student? Student { get; set; }
    public Instructor? Instructor { get; set; }
}
