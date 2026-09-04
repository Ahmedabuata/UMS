using System;
using University.Shared.Common;

namespace University.Core.Entities;

public class UserGroup : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? BranchCode { get; set; }

    public User? User { get; set; }
    public Group? Group { get; set; }
}
