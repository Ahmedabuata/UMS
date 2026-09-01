using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Infrastructure.Security;
using University.Shared.Common;
using University.Shared.DTOs.Auth;

namespace University.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ApplicationDbContext _context;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _context = context;
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return Result<AuthResponseDto>.NotFound("USER_NOT_FOUND", "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            return Result<AuthResponseDto>.Forbidden("USER_INACTIVE", "Account is deactivated.");
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            return Result<AuthResponseDto>.Unauthorized("INVALID_CREDENTIALS");
        }

        user.LastLogin = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        var permissions = await GetPermissionsAsync(user.RoleId);
        var token = _tokenGenerator.GenerateAccessToken(user, permissions);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Token = token,
            RefreshToken = _tokenGenerator.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = UserMapper.ToResponse(user),
            Permissions = permissions.ToList()
        });
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return Result<AuthResponseDto>.Conflict("EMAIL_EXISTS", "Email is already registered.");
        }

        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
        {
            return Result<AuthResponseDto>.Conflict("USERNAME_EXISTS", "Username is already taken.");
        }

        var roleExists = await _context.Roles.AnyAsync(r => r.Id == dto.RoleId && r.IsActive);
        if (!roleExists)
        {
            return Result<AuthResponseDto>.Validation("ROLE_INVALID", "Specified role does not exist.");
        }

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            BranchId = dto.BranchId,
            RoleId = dto.RoleId,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .FirstAsync(u => u.Id == user.Id);

        var permissions = await GetPermissionsAsync(saved.RoleId);
        var token = _tokenGenerator.GenerateAccessToken(saved, permissions);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Token = token,
            RefreshToken = _tokenGenerator.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = UserMapper.ToResponse(saved),
            Permissions = permissions.ToList()
        });
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
    {
        // For this version refresh tokens are simply re-issued on valid re-login.
        // A real implementation stores refresh tokens (e.g. in Redis) and validates them.
        return Result<AuthResponseDto>.Unauthorized("INVALID_REFRESH_TOKEN");
    }

    public async Task<Result<bool>> ValidateTokenAsync(string token)
    {
        var principal = _tokenGenerator.ValidateToken(token);
        if (principal == null)
        {
            return Result<bool>.Unauthorized("INVALID_TOKEN");
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        if (!_passwordHasher.Verify(oldPassword, user.PasswordHash))
        {
            return Result<bool>.Validation("WRONG_PASSWORD", "Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> LogoutAsync(Guid userId)
    {
        // Invalidates refresh token in production; for now returns success.
        return Result<bool>.Success(true);
    }

    private async Task<IEnumerable<string>> GetPermissionsAsync(Guid roleId) =>
        await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == roleId && rp.IsActive)
            .Select(rp => rp.Permission!.PermissionName)
            .Distinct()
            .ToListAsync();
}
