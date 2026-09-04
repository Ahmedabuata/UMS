namespace University.Shared.DTOs.Permissions;

public class UpdatePermissionRequestDto
{
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Module { get; set; }
    public string? ModuleCode { get; set; }
}
