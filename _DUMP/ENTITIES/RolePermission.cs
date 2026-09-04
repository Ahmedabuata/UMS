using University.Shared.Common;

namespace University.Core.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public Guid? GrantedBy { get; set; }
    public DateTime? GrantedAt { get; set; }
    public string? BranchCode { get; set; }

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
