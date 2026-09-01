namespace University.Shared.DTOs.Permissions;

public class PermissionResponseDto
{
    public Guid Id { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Module { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
