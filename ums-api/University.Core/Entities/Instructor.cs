using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class Instructor : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? FacultyId { get; set; }
    public string InstructorNumber { get; set; } = string.Empty;
    public AcademicRank AcademicRank { get; set; } = AcademicRank.LECTURER;
    public string? Specialization { get; set; }

    public User? User { get; set; }
    public Faculty? Faculty { get; set; }
    public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
}
