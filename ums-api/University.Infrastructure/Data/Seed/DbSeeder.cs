using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using University.Core.Entities;
using University.Infrastructure.Security;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Seed;

/// <summary>
/// Main database seeder - Idempotent, can run multiple times safely.
/// Final security model:
/// - ADMIN / SUPER_ADMIN: 38 permissions (all)
/// - SECURITY_ADMIN: 15 permissions (security + user management)
/// - HR_MANAGER: 14 permissions (all HR except HR_SALARY_WRITE - can READ salary, cannot WRITE)
/// - HR_EMPLOYEE: 5 permissions (read-only, no salary)
/// - STUDENT: 4, FACULTY: 5, FINANCE_OFFICER: 4
/// </summary>
public class DbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DbSeeder> _logger;

    // Standard 15 HR permissions (source: HR_ONLY_FINAL_V7_EN.sql)
    // Module = 'HR', IsSensitive = true ONLY for salary permissions
    private static readonly (string Name, bool IsSensitive)[] HrPermissions =
    {
        ("HR_EMPLOYEE_READ", false),
        ("HR_EMPLOYEE_WRITE", false),
        ("HR_EMPLOYEE_STATUS", false),
        ("HR_SALARY_READ", true),   // Sensitive - read salary
        ("HR_SALARY_WRITE", true),  // Sensitive - write salary (restricted)
        ("HR_CONTRACT_READ", false),
        ("HR_CONTRACT_WRITE", false),
        ("HR_ATTENDANCE_READ", false),
        ("HR_ATTENDANCE_WRITE", false),
        ("HR_LEAVE_READ", false),
        ("HR_LEAVE_WRITE", false),
        ("HR_RECRUITMENT_READ", false),
        ("HR_RECRUITMENT_WRITE", false),
        ("HR_EVALUATION_READ", false),
        ("HR_EVALUATION_WRITE", false)
    };

    public DbSeeder(ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<DbSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedCoreAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database seeding skipped because the schema/data is not ready: {Message}", ex.Message);
        }
    }

    // ========================================================================
    // CORE SEEDING - Ordered execution is critical
    // ========================================================================
    private async Task SeedCoreAsync()
    {
        // 1. Seed base lookup tables (roles, modules, permissions, groups, branches)
        await SeedRolesAsync();
        await SeedModulesAsync();
        await SeedPermissionsAsync();
        await SeedGroupsAsync();
        await SeedBranchesAsync();

        // 2. Seed HR module + HR permissions + HR roles mapping
        // Must be before ADMIN/SUPER_ADMIN so they get HR permissions too
        await SeedHrAsync();

        // 3. Seed academic / finance / security roles permissions
        // These were 0 before, now fixed
        await SeedAcademicAndSecurityRolesAsync();

        // 4. Grant ADMIN all permissions (including HR) - Idempotent, always up-to-date
        await SeedAdminPermissionsAsync();

        // 5. Seed SUPER_ADMIN (shared PK across employees, users, instructors)
        await SeedSuperAdminAsync();
    }

    // ------------------------------------------------------------------------
    // 1. BASE TABLES
    // ------------------------------------------------------------------------
    private async Task SeedRolesAsync()
    {
        if (await _context.Roles.AnyAsync()) return;

        _logger.LogInformation("Seeding roles...");
        _context.Roles.AddRange(new List<Role>
        {
            new() { RoleName = "ADMIN", DisplayName = "Administrator", Description = "System Administrator", IsSystemRole = true, IsActive = true },
            new() { RoleName = "STUDENT", DisplayName = "Student", Description = "Student User", IsActive = true },
            new() { RoleName = "FACULTY", DisplayName = "Faculty", Description = "Faculty Member", IsActive = true },
            new() { RoleName = "FINANCE_OFFICER", DisplayName = "Finance Officer", Description = "Finance Officer", IsActive = true },
            new() { RoleName = "SECURITY_ADMIN", DisplayName = "Security Administrator", Description = "Manages users, roles, permissions and security policy", IsSystemRole = true, IsActive = true }
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedModulesAsync()
    {
        if (await _context.Modules.AnyAsync()) return;

        _logger.LogInformation("Seeding modules...");
        _context.Modules.AddRange(new List<Module>
        {
            new() { Code = "AUTH", Name = "Authentication", Description = "Authentication & session", IsActive = true },
            new() { Code = "USER", Name = "User Management", Description = "User accounts", IsActive = true },
            new() { Code = "SECURITY", Name = "Security", Description = "Security manager", IsActive = true },
            new() { Code = "ROLE", Name = "Roles", Description = "Role management", IsActive = true },
            new() { Code = "GROUP", Name = "Groups", Description = "Security groups", IsActive = true },
            new() { Code = "PERMISSION", Name = "Permissions", Description = "Permission catalog", IsActive = true },
            new() { Code = "AUDIT", Name = "Audit Logs", Description = "Audit trail", IsActive = true },
            new() { Code = "POLICY", Name = "Security Policy", Description = "Password & token policy", IsActive = true },
            new() { Code = "STUDENT", Name = "Student", Description = "Student data", IsActive = true },
            new() { Code = "ENROLLMENT", Name = "Enrollment", Description = "Enrollments", IsActive = true },
            new() { Code = "GRADE", Name = "Grade", Description = "Grades", IsActive = true },
            new() { Code = "FINANCE", Name = "Finance", Description = "Financial records", IsActive = true },
            new() { Code = "HR", Name = "Human Resources", Description = "HR data and records", IsActive = true }
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedPermissionsAsync()
    {
        if (await _context.Permissions.AnyAsync()) return;

        _logger.LogInformation("Seeding base permissions...");
        _context.Permissions.AddRange(new List<Permission>
        {
            new() { PermissionName = "AUTH_LOGIN", Description = "Can login", Module = "Authentication", ModuleCode = "AUTH", IsActive = true },
            new() { PermissionName = "USER_READ", Description = "Can read users", Module = "User Management", ModuleCode = "USER", IsActive = true },
            new() { PermissionName = "USER_WRITE", Description = "Can write users", Module = "User Management", ModuleCode = "USER", IsActive = true },
            new() { PermissionName = "STUDENT_READ", Description = "Can read student data", Module = "Student", ModuleCode = "STUDENT", IsActive = true },
            new() { PermissionName = "STUDENT_WRITE", Description = "Can write student data", Module = "Student", ModuleCode = "STUDENT", IsActive = true },
            new() { PermissionName = "ENROLLMENT_READ", Description = "Can read enrollments", Module = "Enrollment", ModuleCode = "ENROLLMENT", IsActive = true },
            new() { PermissionName = "ENROLLMENT_WRITE", Description = "Can enroll courses", Module = "Enrollment", ModuleCode = "ENROLLMENT", IsActive = true },
            new() { PermissionName = "GRADE_READ", Description = "Can read grades", Module = "Grade", ModuleCode = "GRADE", IsActive = true },
            new() { PermissionName = "GRADE_WRITE", Description = "Can write grades", Module = "Grade", ModuleCode = "GRADE", IsActive = true },
            new() { PermissionName = "FINANCE_READ", Description = "Can read finance", Module = "Finance", ModuleCode = "FINANCE", IsActive = true },
            new() { PermissionName = "FINANCE_WRITE", Description = "Can write finance", Module = "Finance", ModuleCode = "FINANCE", IsActive = true },

            // Security permissions
            new() { PermissionName = "SECURITY_USER_READ", Description = "View security users", Module = "Security", ModuleCode = "SECURITY", IsActive = true },
            new() { PermissionName = "SECURITY_USER_WRITE", Description = "Create/edit/delete security users", Module = "Security", ModuleCode = "SECURITY", IsActive = true },
            new() { PermissionName = "SECURITY_USER_UNLOCK", Description = "Unlock users and reset passwords", Module = "Security", ModuleCode = "SECURITY", IsActive = true },
            new() { PermissionName = "SECURITY_ROLE_READ", Description = "View roles", Module = "Security", ModuleCode = "SECURITY", IsActive = true },
            new() { PermissionName = "SECURITY_ROLE_WRITE", Description = "Create/edit/delete roles", Module = "Security", ModuleCode = "SECURITY", IsActive = true },
            new() { PermissionName = "SECURITY_GROUP_READ", Description = "View security groups", Module = "Security", ModuleCode = "SECURITY", IsActive = true },
            new() { PermissionName = "SECURITY_GROUP_WRITE", Description = "Create/edit/delete security groups", Module = "Security", ModuleCode = "SECURITY", IsActive = true },
            new() { PermissionName = "SECURITY_PERMISSION_READ", Description = "View permission catalog", Module = "Security", ModuleCode = "SECURITY", IsActive = true },
            new() { PermissionName = "SECURITY_ROLE_PERMISSIONS", Description = "Assign permissions to roles", Module = "Security", ModuleCode = "SECURITY", IsActive = true },
            new() { PermissionName = "SECURITY_AUDIT_READ", Description = "View audit logs", Module = "Security", ModuleCode = "SECURITY", IsActive = true },
            new() { PermissionName = "SECURITY_POLICY_READ", Description = "View security policy", Module = "Security", ModuleCode = "SECURITY", IsActive = true },
            new() { PermissionName = "SECURITY_POLICY_WRITE", Description = "Update security policy", Module = "Security", ModuleCode = "SECURITY", IsSensitive = true, IsActive = true }
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedGroupsAsync()
    {
        if (await _context.Groups.AnyAsync()) return;

        _logger.LogInformation("Seeding security groups...");
        _context.Groups.Add(new Group
        {
            Name = "security_operations",
            DisplayName = "Security Operations",
            Description = "Default security operations group",
            IsActive = true
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedBranchesAsync()
    {
        if (await _context.Branches.AnyAsync()) return;

        _logger.LogInformation("Seeding branches...");
        _context.Branches.Add(new Branch
        {
            BranchName = "Main Campus",
            BranchCode = "MC",
            IsActive = true
        });
        await _context.SaveChangesAsync();
    }

    // ------------------------------------------------------------------------
    // 2. HR MODULE - 15 permissions, 2 sensitive (salary)
    // ------------------------------------------------------------------------
    private async Task SeedHrAsync()
    {
        // Ensure HR module exists (idempotent)
        if (!await _context.Modules.AnyAsync(m => m.Code == "HR"))
        {
            _context.Modules.Add(new Module
            {
                Code = "HR",
                Name = "Human Resources",
                Description = "HR data and records",
                IsActive = true
            });
            await _context.SaveChangesAsync();
        }

        // Ensure 15 HR permissions exist (EF equivalent of ON CONFLICT DO NOTHING)
        foreach (var (name, isSensitive) in HrPermissions)
        {
            if (await _context.Permissions.AnyAsync(p => p.PermissionName == name)) continue;

            _context.Permissions.Add(new Permission
            {
                PermissionName = name,
                Module = "HR",
                ModuleCode = "HR",
                Description = name,
                IsSensitive = isSensitive,
                IsActive = true
            });
        }
        await _context.SaveChangesAsync();

        // Ensure HR roles exist
        await EnsureRoleAsync("HR_MANAGER", "HR Manager", "Manages HR records - can READ salary but cannot WRITE salary (security policy)");
        await EnsureRoleAsync("HR_EMPLOYEE", "HR Employee", "Read-only HR access - 5 permissions, no salary");

        // HR_MANAGER: 14 permissions - ALL except HR_SALARY_WRITE (security decision)
        await GrantPermissionsToRoleAsync(
            "HR_MANAGER",
            HrPermissions.Where(p => p.Name != "HR_SALARY_WRITE").Select(p => p.Name));

        // HR_EMPLOYEE: 5 permissions - read-only, no salary at all
        await GrantPermissionsToRoleAsync(
            "HR_EMPLOYEE",
            new[] { "HR_EMPLOYEE_READ", "HR_CONTRACT_READ", "HR_ATTENDANCE_READ", "HR_LEAVE_READ", "HR_EVALUATION_READ" });

        // ADMIN & SUPER_ADMIN get all 15 HR permissions (including sensitive) - they are system admins
        await GrantPermissionsToRoleAsync("ADMIN", HrPermissions.Select(p => p.Name));
        await GrantPermissionsToRoleAsync("SUPER_ADMIN", HrPermissions.Select(p => p.Name));
    }

    // ------------------------------------------------------------------------
    // 3. ACADEMIC / FINANCE / SECURITY ROLES - Fix for 0 permissions issue
    // ------------------------------------------------------------------------
    private async Task SeedAcademicAndSecurityRolesAsync()
    {
        // SECURITY_ADMIN: Full security management - 15 permissions
        await GrantPermissionsToRoleAsync("SECURITY_ADMIN", new[]
        {
            "SECURITY_USER_READ", "SECURITY_USER_WRITE", "SECURITY_USER_UNLOCK",
            "SECURITY_ROLE_READ", "SECURITY_ROLE_WRITE",
            "SECURITY_GROUP_READ", "SECURITY_GROUP_WRITE",
            "SECURITY_PERMISSION_READ", "SECURITY_ROLE_PERMISSIONS",
            "SECURITY_AUDIT_READ", "SECURITY_POLICY_READ", "SECURITY_POLICY_WRITE",
            "AUTH_LOGIN", "USER_READ", "USER_WRITE"
        });

        // STUDENT: Basic academic read
        await GrantPermissionsToRoleAsync("STUDENT", new[]
        {
            "AUTH_LOGIN", "STUDENT_READ", "ENROLLMENT_READ", "GRADE_READ"
        });

        // FACULTY: Can read students and manage grades
        await GrantPermissionsToRoleAsync("FACULTY", new[]
        {
            "AUTH_LOGIN", "STUDENT_READ", "ENROLLMENT_READ", "GRADE_READ", "GRADE_WRITE"
        });

        // FINANCE_OFFICER: Finance management
        await GrantPermissionsToRoleAsync("FINANCE_OFFICER", new[]
        {
            "AUTH_LOGIN", "FINANCE_READ", "FINANCE_WRITE", "STUDENT_READ"
        });
    }

    // ------------------------------------------------------------------------
    // 4. ADMIN - Gets ALL permissions (idempotent)
    // ------------------------------------------------------------------------
    private async Task SeedAdminPermissionsAsync()
    {
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "ADMIN");
        if (adminRole == null) return;

        var grantedIds = await _context.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id && rp.IsActive)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var idsToGrant = await _context.Permissions
            .Where(p => p.IsActive && !grantedIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        if (idsToGrant.Count == 0) return;

        _context.RolePermissions.AddRange(idsToGrant.Select(pid => new RolePermission
        {
            RoleId = adminRole.Id,
            PermissionId = pid,
            IsActive = true,
            GrantedAt = DateTime.UtcNow
        }));
        await _context.SaveChangesAsync();
        _logger.LogInformation("Granted {Count} permissions to ADMIN role (now total {Total}).", idsToGrant.Count, grantedIds.Count + idsToGrant.Count);
    }

    // ------------------------------------------------------------------------
    // 5. SUPER_ADMIN - Shared PK pattern (Employee = User = Instructor)
    // ------------------------------------------------------------------------
    private async Task SeedSuperAdminAsync()
    {
        Guid sharedId = Guid.Parse("d3c6d159-0ee7-491a-84f1-b8fe9c0ea7bf");
        Guid superRoleId = Guid.Parse("d47dd084-e73a-4c01-8fca-d1ed0047a5f1");

        // Create SUPER_ADMIN role if not exists
        if (!await _context.Roles.AnyAsync(r => r.Id == superRoleId))
        {
            _context.Roles.Add(new Role
            {
                Id = superRoleId,
                RoleName = "SUPER_ADMIN",
                DisplayName = "Super Admin",
                Description = "Full access super administrator",
                IsSystemRole = true,
                IsActive = true
            });
            await _context.SaveChangesAsync();
        }

        // Ensure SUPER_ADMIN has ALL permissions (fix for initial creation bug)
        // This runs every time, not only on first creation
        var allPermissions = await _context.Permissions.Where(p => p.IsActive).ToListAsync();
        foreach (var perm in allPermissions)
        {
            if (await _context.RolePermissions.AnyAsync(rp => rp.RoleId == superRoleId && rp.PermissionId == perm.Id)) continue;

            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = superRoleId,
                PermissionId = perm.Id,
                IsActive = true,
                GrantedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        // Employee (source of shared UUID)
        if (!await _context.Employees.AnyAsync(e => e.Id == sharedId))
        {
            var deptId = await _context.AdministrativeDepartments
                .Where(d => d.DepartmentCode == "IT")
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync();
            var branchId = await _context.Branches
                .Where(b => b.IsActive)
                .Select(b => (Guid?)b.Id)
                .FirstOrDefaultAsync();

            _context.Employees.Add(new Employee
            {
                Id = sharedId,
                EmployeeNumber = "EMP-ADMIN-001",
                FullName = "System Admin",
                Email = "admin@ums.com",
                DepartmentId = deptId,
                BranchId = branchId,
                ContractType = "Full-time",
                Status = "Active",
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
                IsActive = true
            });
            await _context.SaveChangesAsync();
        }

        // User (same UUID)
        if (!await _context.Users.AnyAsync(u => u.Id == sharedId))
        {
            _context.Users.Add(new User
            {
                Id = sharedId,
                Username = "admin@ums.com",
                PasswordHash = _passwordHasher.Hash("Admin@123"),
                RoleId = superRoleId,
                IsActive = true,
                MustChangePassword = true
            });
            await _context.SaveChangesAsync();
        }

        // Instructor (same UUID)
        if (!await _context.Instructors.AnyAsync(i => i.Id == sharedId))
        {
            var facultyId = await _context.Faculties
                .Select(f => (Guid?)f.Id)
                .FirstOrDefaultAsync();

            _context.Instructors.Add(new Instructor
            {
                Id = sharedId,
                InstructorNumber = "INS-202609-00001",
                FacultyId = facultyId,
                AcademicRank = AcademicRank.PROFESSOR,
                Specialization = "System Administration",
                IsActive = true
            });
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("SuperUser ready with Shared UUID {SharedId}. Login: admin@ums.com / Admin@123", sharedId);
    }

    // ------------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------------
    private async Task EnsureRoleAsync(string roleName, string displayName, string description)
    {
        if (await _context.Roles.AnyAsync(r => r.RoleName == roleName)) return;

        _context.Roles.Add(new Role
        {
            RoleName = roleName,
            DisplayName = displayName,
            Description = description,
            IsActive = true
        });
        await _context.SaveChangesAsync();
    }

    private async Task GrantPermissionsToRoleAsync(string roleName, IEnumerable<string> permissionNames)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null) return;

        foreach (var permissionName in permissionNames)
        {
            var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.PermissionName == permissionName);
            if (permission == null) continue;

            if (await _context.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id)) continue;

            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                IsActive = true,
                GrantedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
    }
}
