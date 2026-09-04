using System;
using University.Shared.Common;

namespace University.Core.Entities;

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? AssignedBy { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? BranchCode { get; set; }

    public User? User { get; set; }
    public Role? Role { get; set; }
}
