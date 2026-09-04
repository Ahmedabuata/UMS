using University.Shared.Enums;

namespace University.Shared.DTOs.Students;

public class CreateStudentRequestDto
{
    public Guid? UserId { get; set; }
    public Guid? MajorId { get; set; }
    public Guid? FacultyId { get; set; }
    public Guid? AcademicDepartmentId { get; set; }
    public string? StudentNumber { get; set; }
    public DateOnly? EnrollmentDate { get; set; }
}