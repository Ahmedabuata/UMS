using University.Shared.Enums;

namespace University.Shared.DTOs.Enrollments;

// CANONICAL - CORE CONTRACT - EXACT DB MATCH
public class EnrollRequestDto
{
    public Guid StudentId { get; set; }
    public Guid SectionId { get; set; }
    public Guid SemesterId { get; set; }
}
