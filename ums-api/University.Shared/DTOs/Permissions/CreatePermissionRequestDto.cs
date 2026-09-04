namespace University.Shared.DTOs.Permissions;

public class CreatePermissionRequestDto
{
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Module { get; set; } = string.Empty;
    public string? ModuleCode { get; set; }
}
