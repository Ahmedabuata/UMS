using University.Shared.Common;

namespace University.Core.Entities;

public class Major : BaseEntity
{
    public Guid DepartmentId { get; set; }
    public string MajorName { get; set; } = string.Empty;
    public string MajorCode { get; set; } = string.Empty;
    public int TotalCreditHours { get; set; } = 130;

    public AcademicDepartment? Department { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
