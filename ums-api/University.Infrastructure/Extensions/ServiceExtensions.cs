using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Core.Interfaces.Services.Security;
using University.Infrastructure.Data;
using University.Infrastructure.Data.Seed;
using University.Infrastructure.Repositories;
using University.Infrastructure.Security;
using University.Infrastructure.Services;

namespace University.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            "Host=localhost;Port=5432;Database=ums_db;Username=ums_user;Password=Ums_Pass_2024!";

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnection));

        // Security
        var jwtSettings = new JwtSettings
        {
            Key = configuration["JWT:Key"] ?? "SuperSecretKeyForUMSProject2024!@#1234567890LongEnough",
            Issuer = configuration["JWT:Issuer"] ?? "UMS.API",
            Audience = configuration["JWT:Audience"] ?? "UMS.Client",
            ExpiryMinutes = int.TryParse(configuration["JWT:ExpiryMinutes"], out var mins) ? mins : 60
        };
        services.AddSingleton(jwtSettings);
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPermissionChecker, PermissionChecker>();
        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, SuperAdminHandler>();
        services.AddHttpContextAccessor();

        // Unit of work + repositories
        services.AddScoped<ApplicationDbContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Domain services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IGradeService, GradeService>();
        services.AddScoped<IFinancialService, FinancialService>();
        services.AddScoped<IAdministrativeDepartmentService, AdministrativeDepartmentService>();
        services.AddScoped<IBuildingService, BuildingService>();
        services.AddScoped<IClassroomService, ClassroomService>();
        services.AddScoped<ISemesterService, SemesterService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IFacultyService, FacultyService>();
        services.AddScoped<IAcademicDepartmentService, AcademicDepartmentService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IIdentifierGeneratorService, IdentifierGeneratorService>();
        services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Security manager services (Layer 2)
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<ISecurityPermissionService, SecurityPermissionService>();
        services.AddScoped<ISecurityPermissionQueryService, SecurityPermissionQueryService>();
        services.AddScoped<ISecurityUserService, SecurityUserService>();
        services.AddScoped<ISecurityRoleService, SecurityRoleService>();
        services.AddScoped<ISecurityGroupService, SecurityGroupService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISecurityPolicyService, SecurityPolicyService>();

        services.AddScoped<DbSeeder>();

        return services;
    }
}
