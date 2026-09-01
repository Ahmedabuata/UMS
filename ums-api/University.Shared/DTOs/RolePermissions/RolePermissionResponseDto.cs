namespace University.Shared.DTOs.RolePermissions;

public class RolePermissionResponseDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public bool IsActive { get; set; }
}
