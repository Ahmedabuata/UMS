namespace University.Shared.DTOs.Semesters;

public class SemesterResponseDto
{
    public Guid Id { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string SemesterCode { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
