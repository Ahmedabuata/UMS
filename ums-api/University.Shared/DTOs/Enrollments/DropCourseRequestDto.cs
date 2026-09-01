namespace University.Shared.DTOs.Enrollments;

public class DropCourseRequestDto
{
    public Guid EnrollmentId { get; set; }
    public string? Reason { get; set; }
}
