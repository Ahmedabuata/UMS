using University.Shared.Enums;

namespace University.Shared.DTOs.Instructors;

public class CreateInstructorRequestDto
{
    public Guid UserId { get; set; }
    public Guid? FacultyId { get; set; }
    public string InstructorNumber { get; set; } = string.Empty;
    public AcademicRank AcademicRank { get; set; }
    public string? Specialization { get; set; }
}
