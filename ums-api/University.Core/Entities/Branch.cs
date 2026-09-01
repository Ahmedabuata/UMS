using University.Shared.Common;

namespace University.Core.Entities;

public class Branch : BaseEntity
{
    public string BranchName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();
    public ICollection<AdministrativeDepartment> AdministrativeDepartments { get; set; } = new List<AdministrativeDepartment>();
    public ICollection<Classroom> Classrooms { get; set; } = new List<Classroom>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
