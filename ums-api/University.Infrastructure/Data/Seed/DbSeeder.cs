using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using University.Core.Entities;
using University.Infrastructure.Security;

namespace University.Infrastructure.Data.Seed;

public class DbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<DbSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (!await _context.Roles.AnyAsync())
        {
            _logger.LogInformation("Seeding roles...");
            _context.Roles.AddRange(new List<Role>
            {
                new() { RoleName = "ADMIN", Description = "System Administrator", IsActive = true },
                new() { RoleName = "STUDENT", Description = "Student User", IsActive = true },
                new() { RoleName = "FACULTY", Description = "Faculty Member", IsActive = true },
                new() { RoleName = "FINANCE_OFFICER", Description = "Finance Officer", IsActive = true }
            });
            await _context.SaveChangesAsync();
        }

        if (!await _context.Permissions.AnyAsync())
        {
            _logger.LogInformation("Seeding permissions...");
            _context.Permissions.AddRange(new List<Permission>
            {
                new() { PermissionName = "AUTH_LOGIN", Description = "Can login", Module = "AUTH", IsActive = true },
                new() { PermissionName = "USER_READ", Description = "Can read users", Module = "USER", IsActive = true },
                new() { PermissionName = "USER_WRITE", Description = "Can write users", Module = "USER", IsActive = true },
                new() { PermissionName = "STUDENT_READ", Description = "Can read student data", Module = "STUDENT", IsActive = true },
                new() { PermissionName = "STUDENT_WRITE", Description = "Can write student data", Module = "STUDENT", IsActive = true },
                new() { PermissionName = "ENROLLMENT_READ", Description = "Can read enrollments", Module = "ENROLLMENT", IsActive = true },
                new() { PermissionName = "ENROLLMENT_WRITE", Description = "Can enroll courses", Module = "ENROLLMENT", IsActive = true },
                new() { PermissionName = "GRADE_READ", Description = "Can read grades", Module = "GRADE", IsActive = true },
                new() { PermissionName = "GRADE_WRITE", Description = "Can write grades", Module = "GRADE", IsActive = true },
                new() { PermissionName = "FINANCE_READ", Description = "Can read finance", Module = "FINANCE", IsActive = true },
                new() { PermissionName = "FINANCE_WRITE", Description = "Can write finance", Module = "FINANCE", IsActive = true }
            });
            await _context.SaveChangesAsync();
        }

        if (!await _context.Branches.AnyAsync())
        {
            _logger.LogInformation("Seeding branches...");
            _context.Branches.Add(new Branch
            {
                BranchName = "Main Campus",
                BranchCode = "MC",
                IsActive = true
            });
            await _context.SaveChangesAsync();
        }

        await SeedAdminUserAsync();
    }

    private async Task SeedAdminUserAsync()
    {
        if (await _context.Users.AnyAsync(u => u.Email == "admin@ums.com"))
        {
            return;
        }

        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "ADMIN");
        if (adminRole == null)
        {
            return;
        }

        var branch = await _context.Branches.FirstOrDefaultAsync();

        _context.Users.Add(new User
        {
            Username = "admin",
            Email = "admin@ums.com",
            PasswordHash = _passwordHasher.Hash("Admin@123"),
            FullName = "System Administrator",
            RoleId = adminRole.Id,
            BranchId = branch?.Id,
            IsActive = true
        });

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded admin user admin@ums.com");
    }
}
