namespace University.Shared.DTOs.AcademicDepartments;

public class AcademicDepartmentResponseDto
{
    public Guid Id { get; set; }
    public Guid FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string? HeadName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}