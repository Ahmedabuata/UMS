namespace University.Shared.DTOs.Employees;

public class EmployeeResponseDto
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? ContractType { get; set; }
    public string? Status { get; set; }
    public DateOnly? HireDate { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public bool? UserIsActive { get; set; }
    public string? AcademicTitle { get; set; }
    public string? Specialization { get; set; }
    public string? FacultyName { get; set; }

    // Read-only derived label (NO DB column): "Academic", "Administrative", or "Both".
    // Academic = has a linked Instructor record; Administrative = has an admin Department.
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TempPassword { get; set; }
}

public class CreateEmployeeRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }
    public string? ContractType { get; set; }
    public string? Status { get; set; }
    public DateOnly? HireDate { get; set; }

    // Optional: create a login account for this employee (employee-first, SAME UUID).
    // No schema change - only controls whether a User row is created alongside the Employee.
    public bool CreateUserAccount { get; set; }
    public Guid? RoleId { get; set; }

    // Optional academic title (e.g. "محاضر"). If set, a linked Instructor row is created
    // with the SAME UUID (existing instructors table). No schema change.
    public string? AcademicTitle { get; set; }
}

public class UpdateEmployeeRequestDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }
    public string? ContractType { get; set; }
    public string? Status { get; set; }
    public string? AcademicTitle { get; set; }
    public DateOnly? HireDate { get; set; }
    public Guid? UserId { get; set; }
    public bool? IsActive { get; set; }
}