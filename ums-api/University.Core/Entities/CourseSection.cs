using University.Shared.Common;

namespace University.Core.Entities;

public class CourseSection : BaseEntity
{
    public Guid CourseId { get; set; }
    public Guid SemesterId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid? InstructorId { get; set; }
    public string SectionNumber { get; set; } = string.Empty;
    public int MaxCapacity { get; set; } = 30;
    public int CurrentEnrollment { get; set; } = 0;
    public string? ScheduleDays { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public Course? Course { get; set; }
    public Semester? Semester { get; set; }
    public Classroom? Classroom { get; set; }
    public Instructor? Instructor { get; set; }
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
}
