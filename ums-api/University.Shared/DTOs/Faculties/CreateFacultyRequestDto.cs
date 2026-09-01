namespace University.Shared.DTOs.Faculties;

public class CreateFacultyRequestDto
{
    public Guid BranchId { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public string FacultyCode { get; set; } = string.Empty;
    public string? DeanName { get; set; }
}
