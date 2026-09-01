namespace University.Shared.DTOs.Sections;

public class CourseSectionResponseDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid SemesterId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid? InstructorId { get; set; }
    public string SectionNumber { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public int CurrentEnrollment { get; set; }
    public string? ScheduleDays { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
