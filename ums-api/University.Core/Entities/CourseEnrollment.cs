using University.Shared.Common;
using University.Shared.Enums;

namespace University.Core.Entities;

public class CourseEnrollment : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid SectionId { get; set; }
    public Guid SemesterId { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.ENROLLED;

    public Student? Student { get; set; }
    public CourseSection? Section { get; set; }
    public Semester? Semester { get; set; }
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public Grade? Grade { get; set; }
}
