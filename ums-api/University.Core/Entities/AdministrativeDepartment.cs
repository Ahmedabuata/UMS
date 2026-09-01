using University.Shared.Common;

namespace University.Core.Entities;

public class AdministrativeDepartment : BaseEntity
{
    public Guid BranchId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Branch? Branch { get; set; }
}
