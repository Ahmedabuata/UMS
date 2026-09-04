using University.Shared.Common;

namespace University.Core.Entities;

public class Employee : BaseEntity
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }
    public string? ContractType { get; set; }
    public string? Status { get; set; }
    public DateOnly? HireDate { get; set; }

    public AdministrativeDepartment? Department { get; set; }
    public Branch? Branch { get; set; }
    public User? User { get; set; }
    public Instructor? Instructor { get; set; }
}