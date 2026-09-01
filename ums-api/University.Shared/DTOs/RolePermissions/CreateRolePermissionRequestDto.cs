namespace University.Shared.DTOs.RolePermissions;

public class CreateRolePermissionRequestDto
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
