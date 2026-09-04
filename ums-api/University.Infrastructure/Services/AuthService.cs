using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Infrastructure.Security;
using University.Shared.Common;
using University.Shared.DTOs.Auth;
using University.Shared.DTOs.Users;

namespace University.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly ApplicationDbContext _context;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IPasswordPolicyService passwordPolicy,
        ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _passwordPolicy = passwordPolicy;
        _context = context;
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(string username, string password)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return Result<AuthResponseDto>.NotFound("USER_NOT_FOUND", "Invalid username or password.");
        }

        if (!user.IsActive)
        {
            return Result<AuthResponseDto>.Forbidden("USER_INACTIVE", "Account is deactivated.");
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            return Result<AuthResponseDto>.Unauthorized("INVALID_CREDENTIALS");
        }

        var permissions = await GetPermissionsAsync(user.RoleId);

        // Password policy flow: a temporary password must be changed before full access is granted.
        if (user.MustChangePassword)
        {
            var limitedToken = _tokenGenerator.GenerateAccessToken(user, permissions);
            return Result<AuthResponseDto>.Success(new AuthResponseDto
            {
                Token = limitedToken,
                RefreshToken = string.Empty,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = await BuildUserDtoAsync(user),
                Permissions = permissions.ToList(),
                MustChangePassword = true
            });
        }

        var token = _tokenGenerator.GenerateAccessToken(user, permissions);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Token = token,
            RefreshToken = _tokenGenerator.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = await BuildUserDtoAsync(user),
            Permissions = permissions.ToList()
        });
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
        {
            return Result<AuthResponseDto>.Conflict("USERNAME_EXISTS", "Username is already taken.");
        }

        var roleExists = await _context.Roles.AnyAsync(r => r.Id == dto.RoleId && r.IsActive);
        if (!roleExists)
        {
            return Result<AuthResponseDto>.Validation("ROLE_INVALID", "Specified role does not exist.");
        }

        var (valid, reason) = _passwordPolicy.ValidatePassword(dto.Password);
        if (!valid)
        {
            return Result<AuthResponseDto>.Validation("WEAK_PASSWORD", reason);
        }

        // Users are employee-backed (shared PK: user.id == employee.id, enforced by the
        // users.id -> employees.id FK with ON DELETE CASCADE). Create the Employee FIRST, then
        // the User with the SAME UUID.
        var userId = Guid.NewGuid();
        var username = dto.Username.Trim();
        var employeeEmail = dto.Email;
        if (string.IsNullOrWhiteSpace(employeeEmail))
        {
            employeeEmail = username.Contains('@')
                ? username
                : $"{username}@ums.local".ToLowerInvariant();
        }

        _context.Employees.Add(new Employee
        {
            Id = userId,
            EmployeeNumber = username,
            FullName = string.IsNullOrWhiteSpace(dto.FullName) ? username : dto.FullName.Trim(),
            Email = employeeEmail,
            Phone = dto.Phone,
            ContractType = "Full-time",
            Status = "Active",
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true
        });

        var user = new User
        {
            Id = userId, // SAME UUID as the employee.
            Username = username,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            RoleId = dto.RoleId,
            IsActive = true,
            MustChangePassword = false
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _context.Users
            .Include(u => u.Role)
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

        var (valid, reason) = _passwordPolicy.ValidatePassword(newPassword);
        if (!valid)
        {
            return Result<bool>.Validation("WEAK_PASSWORD", reason);
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> LogoutAsync(Guid userId)
    {
        return Result<bool>.Success(true);
    }

    private async Task<IEnumerable<string>> GetPermissionsAsync(Guid roleId) =>
        await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == roleId && rp.IsActive)
            .Select(rp => rp.Permission!.PermissionName)
            .Distinct()
            .ToListAsync();

    // Shared-PK resolution: users.id == employees.id == instructors.id (or students.id).
    private async Task<UserResponseDto> BuildUserDtoAsync(User user)
    {
        var dto = UserMapper.ToResponse(user);

        // Employee (instructors are also employees and share the same UUID).
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == user.Id);
        if (employee != null)
        {
            dto.FullName = employee.FullName;
            dto.Email = employee.Email;
            dto.PhoneNumber = employee.Phone;
            dto.IdentifierNumber = employee.EmployeeNumber;
            dto.EmployeeNumber = employee.EmployeeNumber;
            dto.DepartmentName = employee.Department?.DepartmentName;
            dto.BranchName = employee.Branch?.BranchName;
            return dto;
        }

        // Student links to a user via Student.UserId (NOT a shared primary key).
        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (student != null)
        {
            dto.IdentifierNumber = student.StudentNumber;
        }

        return dto;
    }
}