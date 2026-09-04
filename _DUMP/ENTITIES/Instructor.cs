using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class Instructor : BaseEntity
{
    public Guid? FacultyId { get; set; }
    public string InstructorNumber { get; set; } = string.Empty;
    public AcademicRank AcademicRank { get; set; } = AcademicRank.LECTURER;
    public string? Specialization { get; set; }

    // Shared-Primary-Key 1:1: instructor.Id == employee.Id (instructors are employees).
    public Employee? Employee { get; set; }
    public Faculty? Faculty { get; set; }
    public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
}
