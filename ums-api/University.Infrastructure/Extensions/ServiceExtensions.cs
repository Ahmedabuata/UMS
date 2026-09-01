using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
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

        services.AddScoped<DbSeeder>();

        return services;
    }
}
