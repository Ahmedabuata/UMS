namespace University.Shared.DTOs.Roles;

public class CreateRoleRequestDto
{
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
