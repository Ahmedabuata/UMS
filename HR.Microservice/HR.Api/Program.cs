using DotNetCore.CAP;
using HR.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ============================================================
// Swagger with Bearer Authorization
// ============================================================
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste JWT from Identity.Microservice (Bearer token)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// CORS - Frontend
// ============================================================
builder.Services.AddCors(o => o.AddPolicy("AllowFrontend", p =>
    p.WithOrigins("http://localhost:5173", "https://localhost:5173")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// ============================================================
// Database Connection
// Reads from User Secrets (dev) or Environment Variables (prod)
// ============================================================
var conn = builder.Configuration.GetConnectionString("HrConnection")
        ?? builder.Configuration.GetConnectionString("HRDatabase")
        ?? builder.Configuration["ConnectionStrings:HrConnection"];

if (string.IsNullOrWhiteSpace(conn))
{
    throw new InvalidOperationException(
        "Database connection string 'HrConnection' is not configured. " +
        "For development: dotnet user-secrets set \"ConnectionStrings:HrConnection\" \"Host=...\". " +
        "For production: set environment variable ConnectionStrings__HrConnection."
    );
}

builder.Services.AddDbContext<HrDbContext>(o => o.UseNpgsql(conn));

// ============================================================
// CAP (Event Bus) - RabbitMQ
// Reads from User Secrets (dev) or Environment Variables (prod)
// ============================================================
builder.Services.AddCap(o =>
{
    o.UseEntityFramework<HrDbContext>();

    try
    {
        o.UseRabbitMQ(r =>
        {
            r.HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
            r.Port = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var port) ? port : 5672;
            r.UserName = builder.Configuration["RabbitMQ:UserName"] ?? "guest";
            r.Password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
            r.ConnectionFactoryOptions = opt => opt.AutomaticRecoveryEnabled = true;
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[HR.Api] CAP RabbitMQ setup skipped: {ex.Message}");
    }

    o.FailedRetryCount = 5;
    o.DefaultGroupName = "hr.microservice";
});

// ============================================================
// JWT Authentication
// Reads from User Secrets (dev) or Environment Variables (prod)
// MUST be the same key as Identity.Microservice
// ============================================================
var jwtKey = builder.Configuration["JWT:Key"]
          ?? builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT:Key is not configured. " +
        "For development: dotnet user-secrets set \"JWT:Key\" \"YourKeyHere\". " +
        "For production: set environment variable JWT__Key."
    );
}

if (jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        $"JWT:Key must be at least 32 characters. Current length: {jwtKey.Length}"
    );
}

Console.WriteLine($"[HR.Api] Using JWT Key: {jwtKey.Substring(0, 10)}... (length: {jwtKey.Length})");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false;
        o.SaveToken = true;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        // Debug events for 401 issues
        o.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[HR.Api] JWT Auth Failed: {ctx.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine($"[HR.Api] JWT Validated for: {ctx.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            }
        };
    });

// ============================================================
// Authorization Policies
// ============================================================
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("HR_EMPLOYEE_READ", p => p.RequireAssertion(ctx =>
        ctx.User.HasClaim(c => c.Value.Contains("HR_EMPLOYEE_READ")) ||
        ctx.User.HasClaim(c => c.Value.Contains("SUPER_ADMIN"))));

    o.AddPolicy("HR_EMPLOYEE_WRITE", p => p.RequireAssertion(ctx =>
        ctx.User.HasClaim(c => c.Value.Contains("HR_EMPLOYEE_WRITE")) ||
        ctx.User.HasClaim(c => c.Value.Contains("SUPER_ADMIN"))));

    o.AddPolicy("SUPER_ADMIN", p => p.RequireClaim("permissions", "SUPER_ADMIN"));
});

// ============================================================
// Build App
// ============================================================
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ============================================================
// Auto-Migrate Database
// ============================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<HrDbContext>();
        db.Database.Migrate();
        Console.WriteLine("[HR.Api] HR DB migrated successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[HR.Api] Migration failed: {ex.Message}");
    }
}

app.Run();