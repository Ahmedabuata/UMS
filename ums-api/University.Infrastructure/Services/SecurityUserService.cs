using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Services.Security;
using University.Infrastructure.Data;
using University.Infrastructure.Security;
using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.Infrastructure.Services;

public class SecurityUserService : ISecurityUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public SecurityUserService(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<PagedResult<SecurityUserDto>>> SearchAsync(
        string? search = null, string? branchCode = null, string? userType = null,
        bool? active = null, int page = 1, int pageSize = 20, bool? mustChangePwd = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _context.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                (u.Employee != null && (
                    (u.Employee.Email != null && u.Employee.Email.ToLower().Contains(term)) ||
                    u.Employee.FullName.ToLower().Contains(term) ||
                    u.Employee.EmployeeNumber.ToLower().Contains(term))) ||
                (u.Employee != null && u.Employee.Instructor != null &&
                    u.Employee.Instructor.InstructorNumber.ToLower().Contains(term)) ||
                _context.Students.Any(s => s.UserId == u.Id && s.StudentNumber.ToLower().Contains(term)));
        }
        if (active.HasValue)
        {
            query = query.Where(u => u.IsActive == active.Value);
        }
        if (mustChangePwd.HasValue)
        {
            query = query.Where(u => u.MustChangePassword == mustChangePwd.Value);
        }

        var total = await query.CountAsync();
        var users = await query
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<SecurityUserDto>();
        foreach (var user in users)
        {
            items.Add(await ToUserDtoAsync(user));
        }

        return Result<PagedResult<SecurityUserDto>>.Success(new PagedResult<SecurityUserDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = items
        });
    }

    public async Task<Result<SecurityUserDto>> GetByIdAsync(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return Result<SecurityUserDto>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        return Result<SecurityUserDto>.Success(await ToUserDtoAsync(user));
    }

    public async Task<Result<SecurityUserDto>> CreateAsync(CreateSecurityUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return Result<SecurityUserDto>.Validation("FIELDS_REQUIRED", "Username and password are required.");
        }
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
        {
            return Result<SecurityUserDto>.Conflict("USERNAME_EXISTS", "Username already taken.");
        }

        var role = await _context.Roles.FindAsync(dto.RoleId);
        if (role == null)
        {
            return Result<SecurityUserDto>.Validation("ROLE_INVALID", "Specified role does not exist.");
        }

        // Users are employee-backed (shared PK: user.id == employee.id, enforced by the
        // users.id -> employees.id FK with ON DELETE CASCADE). Create the Employee FIRST, then
        // the User with the SAME UUID. EmployeeNumber == Username so ResolveProfileAsync matches.
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
            RoleId = role.Id,
            IsActive = true,
            MustChangePassword = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (dto.Roles != null && dto.Roles.Count > 0)
        {
            _context.UserRoles.AddRange(dto.Roles.Distinct().Select(roleId => new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            }));
            await _context.SaveChangesAsync();
        }

        if (dto.GroupIds != null && dto.GroupIds.Count > 0)
        {
            _context.UserGroups.AddRange(dto.GroupIds.Distinct().Select(groupId => new UserGroup
            {
                UserId = user.Id,
                GroupId = groupId,
                AssignedAt = DateTime.UtcNow
            }));
            await _context.SaveChangesAsync();
        }

        return Result<SecurityUserDto>.Success(await ToUserDtoAsync(user));
    }

    // Create a login account for an EXISTING employee, reusing the employee's UUID as the
    // user id (shared PK: users.id = employees.id). Security Manager / SuperAdmin only flow.
    public async Task<Result<SecurityUserDto>> CreateForEmployeeAsync(Guid employeeId, CreateUserForEmployeeDto dto)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            return Result<SecurityUserDto>.NotFound("EMPLOYEE_NOT_FOUND", "Create employee in HR first.");
        }
        if (await _context.Users.AnyAsync(u => u.Id == employeeId))
        {
            return Result<SecurityUserDto>.Conflict("ALREADY_HAS_ACCOUNT", "Already has an account, use Reset Password.");
        }
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return Result<SecurityUserDto>.Validation("USERNAME_REQUIRED", "Username is required.");
        }
        var username = dto.Username.Trim();
        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            return Result<SecurityUserDto>.Conflict("USERNAME_EXISTS", "Username already in use.");
        }
        if (dto.RoleId == Guid.Empty || !await _context.Roles.AnyAsync(r => r.Id == dto.RoleId))
        {
            return Result<SecurityUserDto>.Validation("ROLE_INVALID", "Specified role does not exist.");
        }

        // CRITICAL: reuse the employee's UUID so users.id = employees.id (shared PK).
        var user = new User
        {
            Id = employeeId, // SAME UUID as the employee - not Guid.NewGuid().
            Username = username,
            PasswordHash = _passwordHasher.Hash(dto.TempPassword),
            RoleId = dto.RoleId,
            IsActive = true,
            MustChangePassword = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = null,
            Action = "CREATE_USER_ACCOUNT_FOR_EMPLOYEE",
            Entity = "Security",
            EntityId = employeeId.ToString(),
            NewValues = $"Created user '{username}' for employee '{employee.EmployeeNumber}'.",
            CreatedBy = employeeId
        });
        await _context.SaveChangesAsync();

        return Result<SecurityUserDto>.Success(await ToUserDtoAsync(user));
    }

    public async Task<Result<SecurityUserDto>> UpdateAsync(Guid id, UpdateSecurityUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Result<SecurityUserDto>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        if (dto.RoleId.HasValue) user.RoleId = dto.RoleId.Value;
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Result<SecurityUserDto>.Success(await ToUserDtoAsync(user));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ActivateAsync(Guid id, bool activate)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        user.IsActive = activate;
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> UnlockAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ResetPasswordAsync(Guid id, string newPassword)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> SetRolesAsync(Guid id, SetUserRolesDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        var existing = await _context.UserRoles
            .Where(ur => ur.UserId == id)
            .ToListAsync();
        _context.UserRoles.RemoveRange(existing);

        var roleIds = dto.RoleIds.Distinct().ToList();
        if (roleIds.Count > 0)
        {
            _context.UserRoles.AddRange(roleIds.Select(roleId => new UserRole
            {
                UserId = id,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            }));
        }

        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<SecurityRoleDto>>> GetUserRolesAsync(Guid id)
    {
        var userIds = await _context.UserRoles
            .Where(ur => ur.UserId == id && ur.IsActive)
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToListAsync();

        var roles = await _context.Roles
            .Where(r => userIds.Contains(r.Id) && r.IsActive)
            .Select(r => new SecurityRoleDto
            {
                Id = r.Id,
                Name = r.RoleName,
                DisplayName = r.DisplayName ?? r.RoleName,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                IsActive = r.IsActive,
                BranchCode = r.BranchCode
            })
            .ToListAsync();

        return Result<IEnumerable<SecurityRoleDto>>.Success(roles);
    }

    public async Task<Result<IEnumerable<SecurityGroupDto>>> GetUserGroupsAsync(Guid id)
    {
        var groupIds = await _context.UserGroups
            .Where(ug => ug.UserId == id && ug.IsActive)
            .Select(ug => ug.GroupId)
            .Distinct()
            .ToListAsync();

        var groups = await _context.Groups
            .Where(g => groupIds.Contains(g.Id) && g.IsActive)
            .Select(g => new SecurityGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                DisplayName = g.DisplayName ?? g.Name,
                Description = g.Description,
                IsActive = g.IsActive,
                BranchCode = g.BranchCode
            })
            .ToListAsync();

        return Result<IEnumerable<SecurityGroupDto>>.Success(groups);
    }

    public async Task<Result<bool>> AddToGroupAsync(Guid id, Guid groupId)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        var group = await _context.Groups.FindAsync(groupId);
        if (group == null)
        {
            return Result<bool>.NotFound("GROUP_NOT_FOUND", "Group not found.");
        }

        var exists = await _context.UserGroups.AnyAsync(ug =>
            ug.UserId == id && ug.GroupId == groupId && ug.IsActive);
        if (exists)
        {
            return Result<bool>.Conflict("ALREADY_MEMBER", "User is already in this group.");
        }

        _context.UserGroups.Add(new UserGroup
        {
            UserId = id,
            GroupId = groupId,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> RemoveFromGroupAsync(Guid id, Guid groupId)
    {
        var ug = await _context.UserGroups.FirstOrDefaultAsync(link =>
            link.UserId == id && link.GroupId == groupId && link.IsActive);
        if (ug == null)
        {
            return Result<bool>.NotFound("NOT_MEMBER", "User is not in this group.");
        }

        ug.IsActive = false;
        ug.UpdatedAt = DateTime.UtcNow;
        _context.UserGroups.Update(ug);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    private async Task<SecurityUserDto> ToUserDtoAsync(User user)
    {
        var roles = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == user.Id && ur.IsActive)
            .Select(ur => ur.Role!.RoleName)
            .ToListAsync();

        var groups = await _context.UserGroups
            .Include(ug => ug.Group)
            .Where(ug => ug.UserId == user.Id && ug.IsActive)
            .Select(ug => ug.Group!.Name)
            .ToListAsync();

        var profile = await ResolveProfileAsync(user);

        return new SecurityUserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = profile.FullName,
            Email = profile.Email,
            PhoneNumber = profile.PhoneNumber,
            IdentifierNumber = profile.IdentifierNumber,
            DepartmentName = profile.DepartmentName,
            BranchName = profile.BranchName,
            LinkedEntity = profile.LinkedEntity,
            RoleName = user.Role?.RoleName ?? string.Empty,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt,
            Roles = roles,
            Groups = groups
        };
    }

    private async Task<ProfileSnapshot> ResolveProfileAsync(User user)
    {
        // Shared PK: employees.id == users.id. Personal data lives on the employee record
        // keyed by the same UUID (do NOT require EmployeeNumber == Username; login usernames
        // may be the email while the employee number is a friendly code like ADM-HR-...).
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == user.Id);
        if (employee != null)
        {
            return new ProfileSnapshot
            {
                FullName = employee.FullName,
                Email = employee.Email,
                PhoneNumber = employee.Phone,
                IdentifierNumber = employee.EmployeeNumber,
                DepartmentName = employee.Department?.DepartmentName,
                BranchName = employee.Branch?.BranchName,
                LinkedEntity = "Employee Record"
            };
        }

        // Instructors: personal data lives on their shared-PK employee record (instructor.id == employee.id).
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
                    BranchName = empLinked.Branch?.BranchName,
                    LinkedEntity = "Instructor Record"
                };
            }
            return new ProfileSnapshot
            {
                IdentifierNumber = instructor.InstructorNumber,
                LinkedEntity = "Instructor Record"
            };
        }

        // Students link to a user via Student.UserId (NOT a shared primary key).
        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (student != null)
        {
            return new ProfileSnapshot
            {
                IdentifierNumber = student.StudentNumber,
                LinkedEntity = "Student Record"
            };
        }

        return new ProfileSnapshot();
    }

    private sealed class ProfileSnapshot
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IdentifierNumber { get; set; }
        public string? DepartmentName { get; set; }
        public string? BranchName { get; set; }
        public string? LinkedEntity { get; set; }
    }
}