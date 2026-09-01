namespace University.Shared.DTOs.AdministrativeDepartments;

public class AdministrativeDepartmentResponseDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
