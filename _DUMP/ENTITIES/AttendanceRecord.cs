using University.Shared.Common;

namespace University.Core.Entities;

public class AttendanceRecord : BaseEntity
{
    public Guid EnrollmentId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public bool IsPresent { get; set; }
    public string? Notes { get; set; }

    public CourseEnrollment? Enrollment { get; set; }
}
