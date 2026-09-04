using University.Shared.Common;

namespace University.Core.Entities;

public class Module : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BranchCode { get; set; }

    public ICollection<Permission>? Permissions { get; set; }
}
