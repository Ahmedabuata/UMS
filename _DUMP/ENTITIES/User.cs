using University.Shared.Common;

namespace University.Core.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public bool MustChangePassword { get; set; }

    public Role? Role { get; set; }

    // Shared-Primary-Key 1:1: users.id == employees.id (user account for an employee).
    // Employee is the principal; deleting an Employee deletes this User, but deleting this
    // User does NOT delete the Employee.
    public Employee? Employee { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
}
