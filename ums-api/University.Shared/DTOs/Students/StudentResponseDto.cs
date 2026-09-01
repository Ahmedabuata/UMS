using University.Shared.Enums;

namespace University.Shared.DTOs.Students;

public class StudentResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid? MajorId { get; set; }
    public string? MajorName { get; set; }
    public decimal Gpa { get; set; }
    public int CompletedCredits { get; set; }
    public StudentStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
