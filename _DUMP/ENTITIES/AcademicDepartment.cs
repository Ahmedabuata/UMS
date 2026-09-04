using University.Shared.Common;

namespace University.Core.Entities;

public class AcademicDepartment : BaseEntity
{
    public Guid FacultyId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string? HeadName { get; set; }
    public string? Description { get; set; }

    public Faculty? Faculty { get; set; }
    public ICollection<Major> Majors { get; set; } = new List<Major>();
}
