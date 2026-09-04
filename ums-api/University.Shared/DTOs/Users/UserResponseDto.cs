namespace University.Shared.DTOs.Users;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Read-only personal data joined from employees/students/instructors (not stored on users).
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? IdentifierNumber { get; set; }

    // Read-only computed alias for the current user's employee number (shared-PK join, no DB column).
    public string? EmployeeNumber { get; set; }
    public string? DepartmentName { get; set; }
    public string? BranchName { get; set; }
}
