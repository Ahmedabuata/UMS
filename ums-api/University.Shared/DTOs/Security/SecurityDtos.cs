namespace University.Shared.DTOs.Security;

public class PagedResult<T>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
}

public class SecurityUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;

    // Read-only personal data joined from employees/students/instructors.
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? IdentifierNumber { get; set; }
    public string? DepartmentName { get; set; }
    public string? BranchName { get; set; }
    public string? RoleName { get; set; }
    public string? LinkedEntity { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Groups { get; set; } = new();
}

public class CreateSecurityUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? IdentifierNumber { get; set; }
    public Guid RoleId { get; set; }
    public List<Guid> Roles { get; set; } = new();
    public List<Guid> GroupIds { get; set; } = new();

    // Employee profile. Users are employee-backed (shared PK: user.id == employee.id), so a
    // linked employee record is required. If omitted, sensible defaults are derived.
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }
}

public class UpdateSecurityUserDto
{
    public Guid? RoleId { get; set; }
}

public class SecurityRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public string? BranchCode { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class CreateSecurityRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? BranchCode { get; set; }
}

public class UpdateSecurityRoleDto
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class AssignPermissionsDto
{
    public List<Guid> PermissionIds { get; set; } = new();
}

public class SecurityGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? BranchCode { get; set; }
}

public class CreateSecurityGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? BranchCode { get; set; }
}

public class UpdateSecurityGroupDto
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class GroupMemberDto
{
    public Guid UserId { get; set; }
}

public class PermissionItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ModuleCode { get; set; }
    public string Module { get; set; } = string.Empty;
    public bool IsSensitive { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PermissionByModuleDto
{
    public string Module { get; set; } = string.Empty;
    public string? ModuleCode { get; set; }
    public List<PermissionItemDto> Permissions { get; set; } = new();
}

public class ModuleItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AuditLogItemDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Action { get; set; }
    public string? Entity { get; set; }
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? BranchCode { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}

public class SetUserRolesDto
{
    public List<Guid> RoleIds { get; set; } = new();
}

public class ResetPasswordRequestDto
{
    public string NewPassword { get; set; } = string.Empty;
}

// Employee picker option for "Create User Account for Existing Employee".
public class EmployeeAccountOptionDto
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
}

// Create a login account for an EXISTING employee, reusing the employee's UUID (shared PK).
public class CreateUserForEmployeeDto
{
    public string Username { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string TempPassword { get; set; } = string.Empty;
}
