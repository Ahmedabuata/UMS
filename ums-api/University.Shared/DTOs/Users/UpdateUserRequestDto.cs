namespace University.Shared.DTOs.Users;

public class UpdateUserRequestDto
{
    public Guid? RoleId { get; set; }
    public bool? IsActive { get; set; }
}
