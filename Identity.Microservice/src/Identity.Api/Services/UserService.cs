using System.Text.Json;
using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Identity.Api.Validators;
using Identity.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Api.Services;

/// <summary>
/// Implements user management operations.
/// </summary>
public class UserService : IUserService
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IdentityDbContext db,
        ITokenService tokenService,
        ILogger<UserService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return null;

        return await MapToUserDtoAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserListResponse> GetUsersAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(s)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = new List<UserDto>();
        foreach (var user in users)
        {
            dtos.Add(await MapToUserDtoAsync(user, cancellationToken));
        }

        return new UserListResponse(dtos, total, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var inputErr = GlobalValidators.ValidateUserInput(request.Email, request.Username, request.PhoneNumber);
        if (inputErr != null)
            throw new ArgumentException(inputErr);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long.");

        var emailNorm = GlobalValidators.NormalizeEmail(request.Email);
        var userNorm = GlobalValidators.NormalizeUsername(request.Username);

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm.ToLower(), cancellationToken))
            throw new InvalidOperationException("Email already exists.");

        if (await _db.Users.AnyAsync(u => u.Username == userNorm, cancellationToken))
            throw new InvalidOperationException("Username already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = userNorm,
            Email = emailNorm,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber?.Trim(),
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            IsActive = true,
            MustChangePassword = false,
            FailedLoginAttempts = 0,
            LockoutEnd = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        if (!string.IsNullOrWhiteSpace(request.RoleName))
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == request.RoleName.Trim().ToUpperInvariant(), cancellationToken);
            if (role != null)
            {
                _db.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await MapToUserDtoAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var oldValues = new { user.Email, user.Username, user.PhoneNumber, user.FirstName, user.LastName, user.IsActive };

        if (request.Email != null)
        {
            if (!GlobalValidators.IsValidEmail(request.Email))
                throw new ArgumentException("Invalid email format.");

            var norm = GlobalValidators.NormalizeEmail(request.Email);
            if (await _db.Users.AnyAsync(x => x.Id != userId && x.Email.ToLower() == norm.ToLower(), cancellationToken))
                throw new InvalidOperationException("Email already exists.");

            user.Email = norm;
        }

        if (request.Username != null)
        {
            if (!GlobalValidators.IsValidUsername(request.Username))
                throw new ArgumentException("Invalid username format.");

            var norm = GlobalValidators.NormalizeUsername(request.Username);
            if (await _db.Users.AnyAsync(x => x.Id != userId && x.Username == norm, cancellationToken))
                throw new InvalidOperationException("Username already exists.");

            user.Username = norm;
        }

        if (request.PhoneNumber != null)
            user.PhoneNumber = request.PhoneNumber.Trim();

        if (request.FirstName != null)
            user.FirstName = request.FirstName.Trim();

        if (request.LastName != null)
            user.LastName = request.LastName.Trim();

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return await MapToUserDtoAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserDto> PatchUserStatusAsync(Guid userId, PatchUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!request.IsActive)
        {
            await _tokenService.RevokeAllUserTokensAsync(userId);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await MapToUserDtoAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null || !user.IsActive)
            return false;

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return false;

        var passwordErr = GlobalValidators.ValidatePassword(request.NewPassword);
        if (passwordErr != null)
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _tokenService.RevokeAllUserTokensAsync(userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            return false;

        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.Role != null && ur.Role.RoleName == "SUPER_ADMIN", cancellationToken);

        if (isSuperAdmin)
            throw new InvalidOperationException("Cannot delete user with SUPER_ADMIN role.");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _tokenService.RevokeAllUserTokensAsync(userId);

        return true;
    }

    /// <inheritdoc />
    public async Task AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            throw new KeyNotFoundException("User not found.");

        var roleExists = await _db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
        if (!roleExists)
            throw new KeyNotFoundException("Role not found.");

        var alreadyAssigned = await _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        if (alreadyAssigned)
            throw new InvalidOperationException("Role already assigned to user.");

        _db.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow
        });

        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user != null)
            user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _tokenService.RevokeAllUserTokensAsync(userId);
    }

    /// <inheritdoc />
    public async Task UnassignRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _db.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole == null)
            throw new KeyNotFoundException("User role assignment not found.");

        if (userRole.Role != null && userRole.Role.RoleName == "SUPER_ADMIN")
        {
            var otherAdmin = await _db.UserRoles
                .AnyAsync(ur => ur.RoleId == roleId && ur.UserId != userId, cancellationToken);

            if (!otherAdmin)
                throw new InvalidOperationException("Cannot remove the last SUPER_ADMIN user.");
        }

        _db.UserRoles.Remove(userRole);

        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user != null)
            user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _tokenService.RevokeAllUserTokensAsync(userId);
    }

    /// <inheritdoc />
    public async Task AssignGroupToUserAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            throw new KeyNotFoundException("User not found.");

        var groupExists = await _db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken);
        if (!groupExists)
            throw new KeyNotFoundException("Group not found.");

        var alreadyAssigned = await _db.UserGroups.AnyAsync(ug => ug.UserId == userId && ug.GroupId == groupId, cancellationToken);
        if (alreadyAssigned)
            throw new InvalidOperationException("User already in group.");

        _db.UserGroups.Add(new UserGroup
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GroupId = groupId,
            JoinedAt = DateTime.UtcNow
        });

        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user != null)
            user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UnassignGroupFromUserAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var userGroup = await _db.UserGroups
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId, cancellationToken);

        if (userGroup == null)
            throw new KeyNotFoundException("User group assignment not found.");

        _db.UserGroups.Remove(userGroup);

        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user != null)
            user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<RoleDto>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            throw new KeyNotFoundException("User not found.");

        return await _db.UserRoles
            .Where(ur => ur.UserId == userId && ur.Role != null)
            .Select(ur => new RoleDto(
                ur.Role!.Id,
                ur.Role.RoleName,
                ur.Role.DisplayName ?? ur.Role.RoleName,
                ur.Role.Description,
                ur.Role.BranchCode,
                ur.Role.IsSystemRole,
                ur.Role.IsActive,
                ur.Role.CreatedAt,
                ur.Role.UpdatedAt,
                0))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<GroupDto>> GetUserGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            throw new KeyNotFoundException("User not found.");

        return await _db.UserGroups
            .Where(ug => ug.UserId == userId && ug.Group != null)
            .Select(ug => new GroupDto(
                ug.Group!.Id,
                ug.Group.Name,
                ug.Group.DisplayName ?? ug.Group.Name,
                ug.Group.Description,
                ug.Group.BranchCode,
                ug.Group.IsActive,
                ug.Group.CreatedAt,
                ug.Group.UpdatedAt  // ✅ صحيح (بدون ??)
            ))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile == null)
            return null;

        return new UserProfileDto(
            profile.Id,
            profile.UserId,
            profile.Address,
            profile.City,
            profile.PostalCode,
            profile.MaritalStatus,
            profile.DateOfBirth,
            profile.Gender,
            profile.CreatedAt,
            profile.UpdatedAt
        );
    }

    #region Private Helpers

    private async Task<UserDto> MapToUserDtoAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id && ur.Role != null)
            .Select(ur => ur.Role!.RoleName)
            .ToListAsync(cancellationToken);

        var permissions = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p != null ? p.PermissionName : string.Empty)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToListAsync(cancellationToken);

        var groups = await _db.UserGroups
            .Where(ug => ug.UserId == user.Id && ug.Group != null)
            .Select(ug => ug.Group!.Name)
            .ToListAsync(cancellationToken);

        var profile = await GetUserProfileAsync(user.Id, cancellationToken);

        return new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.PhoneNumber,
            user.IsActive,
            user.MustChangePassword,
            user.FailedLoginAttempts,
            user.LockoutEnd,
            user.FirstName,
            user.LastName,
            user.CreatedAt,
            user.UpdatedAt,
            roles,
            permissions,
            groups,
            profile
        );
    }

    #endregion
}