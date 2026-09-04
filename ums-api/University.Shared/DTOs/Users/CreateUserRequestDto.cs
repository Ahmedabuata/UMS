namespace University.Shared.DTOs.Users;

public class CreateUserRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid RoleId { get; set; }

    // Employee profile. Users are employee-backed (shared PK: user.id == employee.id), so a
    // linked employee record is created. If omitted, sensible defaults are derived.
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }
}
