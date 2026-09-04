using University.Shared.Common;

namespace University.Core.Entities;

public class Permission : BaseEntity
{
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Module { get; set; } = string.Empty;
    public string? ModuleCode { get; set; }
    public bool IsSensitive { get; set; }
    public string? BranchCode { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
