namespace University.Shared.DTOs.Auth;

public class RegisterRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Guid RoleId { get; set; }
    public Guid? BranchId { get; set; }
}
