namespace University.Shared.DTOs.Faculties;

public class CreateFacultyRequestDto
{
    public string FacultyName { get; set; } = string.Empty;
    public string FacultyCode { get; set; } = string.Empty;
    public string? DeanName { get; set; }
    public Guid BranchId { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
}