using University.Shared.Common;
using University.Shared.DTOs.Auth;

namespace University.Core.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> LoginAsync(string email, string password);
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken);
    Task<Result<bool>> ValidateTokenAsync(string token);
    Task<Result<bool>> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
    Task<Result<bool>> LogoutAsync(Guid userId);
}
