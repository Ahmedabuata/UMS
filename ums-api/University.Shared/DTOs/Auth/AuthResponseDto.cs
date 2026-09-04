using University.Shared.DTOs.Users;

namespace University.Shared.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserResponseDto? User { get; set; }
    public List<string> Permissions { get; set; } = new();
    public bool MustChangePassword { get; set; }
}
