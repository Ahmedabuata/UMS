namespace University.Shared.DTOs.AdministrativeDepartments;

public class CreateAdministrativeDepartmentRequestDto
{
    public Guid BranchId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string? Description { get; set; }
}
