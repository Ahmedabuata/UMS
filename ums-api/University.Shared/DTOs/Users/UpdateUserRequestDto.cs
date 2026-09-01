namespace University.Shared.DTOs.Users;

public class UpdateUserRequestDto
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? RoleId { get; set; }
    public bool? IsActive { get; set; }
}
