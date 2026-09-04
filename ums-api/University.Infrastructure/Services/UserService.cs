using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Infrastructure.Security;
using University.Shared.Common;
using University.Shared.DTOs.Users;

namespace University.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly ApplicationDbContext _context;

    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IPasswordPolicyService passwordPolicy,
        ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
        _context = context;
    }

    public async Task<Result<UserResponseDto>> GetUserByIdAsync(Guid id)
    {
        var user = await _unitOfWork.UserRepository.GetWithRoleAsync(id);
        if (user == null)
        {
            return Result<UserResponseDto>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        var profile = await ResolveProfileAsync(user);
        return Result<UserResponseDto>.Success(new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            RoleName = user.Role?.RoleName ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            FullName = profile.FullName,
            Email = profile.Email,
            PhoneNumber = profile.PhoneNumber,
            IdentifierNumber = profile.IdentifierNumber,
            DepartmentName = profile.DepartmentName,
            BranchName = profile.BranchName
        });
    }

    public async Task<Result<UserResponseDto>> CreateUserAsync(CreateUserRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return Result<UserResponseDto>.Validation("FIELDS_REQUIRED", "Username and password are required.");
        }
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
        {
            return Result<UserResponseDto>.Conflict("USERNAME_EXISTS", "Username already taken.");
        }
        if (dto.RoleId == Guid.Empty || !await _context.Roles.AnyAsync(r => r.Id == dto.RoleId))
        {
            return Result<UserResponseDto>.Validation("ROLE_INVALID", "Specified role does not exist.");
        }
        var (valid, reason) = _passwordPolicy.ValidatePassword(dto.Password);
        if (!valid)
        {
            return Result<UserResponseDto>.Validation("WEAK_PASSWORD", reason);
        }

        // Users are employee-backed (shared PK: user.id == employee.id, enforced by the
        // users.id -> employees.id FK with ON DELETE CASCADE). Create the Employee FIRST, then
        // the User with the SAME UUID. ResolveProfileAsync matches on e.Id == user.Id.
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
            DepartmentId = dto.DepartmentId,
            BranchId = dto.BranchId,
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
            MustChangePassword = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _unitOfWork.UserRepository.GetWithRoleAsync(user.Id);
        var profile = await ResolveProfileAsync(saved!);
        return Result<UserResponseDto>.Success(BuildDto(saved!, profile));
    }

    public async Task<Result<UserResponseDto>> UpdateUserAsync(Guid id, UpdateUserRequestDto dto)
    {
        var user = await _unitOfWork.UserRepository.GetWithRoleAsync(id);
        if (user == null)
        {
            return Result<UserResponseDto>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        if (dto.RoleId.HasValue) user.RoleId = dto.RoleId.Value;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _unitOfWork.UserRepository.GetWithRoleAsync(id);
        var profile = await ResolveProfileAsync(saved!);
        return Result<UserResponseDto>.Success(BuildDto(saved!, profile));
    }

    public async Task<Result<bool>> DeleteUserAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        await _unitOfWork.Users.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> AssignRoleAsync(Guid userId, Guid roleId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private async Task<ProfileSnapshot> ResolveProfileAsync(User user)
    {
        // Shared PK: employees.id == users.id == instructors.id (instructors are employees).
        // Personal data lives on the employee/student record keyed by the same UUID.
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == user.Id && e.EmployeeNumber == user.Username);
        if (employee != null)
        {
            return new ProfileSnapshot
            {
                FullName = employee.FullName,
                Email = employee.Email,
                PhoneNumber = employee.Phone,
                IdentifierNumber = employee.EmployeeNumber,
                DepartmentName = employee.Department?.DepartmentName,
                BranchName = employee.Branch?.BranchName
            };
        }

        var instructor = await _context.Instructors
            .Include(i => i.Employee)
                .ThenInclude(emp => emp!.Department)
            .Include(i => i.Employee)
                .ThenInclude(emp => emp!.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == user.Id);
        if (instructor != null)
        {
            var empLinked = instructor.Employee;
            if (empLinked != null)
            {
                return new ProfileSnapshot
                {
                    FullName = empLinked.FullName,
                    Email = empLinked.Email,
                    PhoneNumber = empLinked.Phone,
                    IdentifierNumber = instructor.InstructorNumber,
                    DepartmentName = empLinked.Department?.DepartmentName,
                    BranchName = empLinked.Branch?.BranchName
                };
            }
            return new ProfileSnapshot { IdentifierNumber = instructor.InstructorNumber };
        }

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (student != null)
        {
            return new ProfileSnapshot { IdentifierNumber = student.StudentNumber };
        }

        return new ProfileSnapshot();
    }

    private static UserResponseDto BuildDto(User user, ProfileSnapshot profile) => new()
    {
        Id = user.Id,
        Username = user.Username,
        RoleName = user.Role?.RoleName ?? string.Empty,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        FullName = profile.FullName,
        Email = profile.Email,
        PhoneNumber = profile.PhoneNumber,
        IdentifierNumber = profile.IdentifierNumber,
        DepartmentName = profile.DepartmentName,
        BranchName = profile.BranchName
    };

    private sealed class ProfileSnapshot
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IdentifierNumber { get; set; }
        public string? DepartmentName { get; set; }
        public string? BranchName { get; set; }
    }
}