namespace University.Shared.DTOs.AdministrativeDepartments;

public class UpdateAdministrativeDepartmentRequestDto
{
    public string? DepartmentName { get; set; }
    public string? DepartmentCode { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
