namespace University.Shared.DTOs.Roles;

public class UpdateRoleRequestDto
{
    public string? RoleName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
