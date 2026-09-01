namespace University.Shared.DTOs.Faculties;

public class FacultyResponseDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public string FacultyCode { get; set; } = string.Empty;
    public string? DeanName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
