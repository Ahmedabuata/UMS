namespace University.Shared.DTOs.Faculties;

public class FacultyResponseDto
{
    public Guid Id { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public string FacultyCode { get; set; } = string.Empty;
    public string? DeanName { get; set; }
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? BranchCode { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public int DepartmentsCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}