using University.Shared.Enums;

namespace University.Shared.DTOs.Enrollments;

public class EnrollmentResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string? StudentNumber { get; set; }
    public string? StudentName { get; set; }
    public Guid SectionId { get; set; }
    public string? SectionNumber { get; set; }
    public Guid CourseId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseName { get; set; }
    public Guid SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public EnrollmentStatus Status { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
