namespace University.Shared.DTOs.AcademicDepartments;

public class CreateAcademicDepartmentRequestDto
{
    public Guid FacultyId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string? HeadName { get; set; }
}
