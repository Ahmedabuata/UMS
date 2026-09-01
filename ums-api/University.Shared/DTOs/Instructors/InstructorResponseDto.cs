using University.Shared.Enums;

namespace University.Shared.DTOs.Instructors;

public class InstructorResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? FacultyId { get; set; }
    public string InstructorNumber { get; set; } = string.Empty;
    public AcademicRank AcademicRank { get; set; }
    public string? Specialization { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
