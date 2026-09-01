using University.Shared.Common;

namespace University.Core.Entities;

public class Faculty : BaseEntity
{
    public Guid BranchId { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public string FacultyCode { get; set; } = string.Empty;
    public string? DeanName { get; set; }

    public Branch? Branch { get; set; }
    public ICollection<AcademicDepartment> AcademicDepartments { get; set; } = new List<AcademicDepartment>();
    public ICollection<Instructor> Instructors { get; set; } = new List<Instructor>();
}
