using University.Shared.Enums;

namespace University.Shared.DTOs.Students;

public class UpdateStudentRequestDto
{
    public Guid? MajorId { get; set; }
    public StudentStatus? Status { get; set; }
    public decimal? Gpa { get; set; }
    public int? CompletedCredits { get; set; }
}
