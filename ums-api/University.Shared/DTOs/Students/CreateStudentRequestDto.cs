using University.Shared.Enums;

namespace University.Shared.DTOs.Students;

public class CreateStudentRequestDto
{
    public Guid UserId { get; set; }
    public Guid MajorId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public DateOnly? EnrollmentDate { get; set; }
}
