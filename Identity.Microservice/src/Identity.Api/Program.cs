using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Identity.Api.Configurations;
using Identity.Api.Data;
using Identity.Api.Interfaces;
using Identity.Api.Middleware;
using Identity.Api.Services;
using Identity.Api.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. Database Context
// =========================================================================
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb")));

// =========================================================================
// 2. Custom Services Registration
// =========================================================================
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuditArchiveService, AuditArchiveService>();

// =========================================================================
// 2.1 Email + Password Generator - GOLDEN V10 NEW 
// =========================================================================
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<IPasswordGenerator, PasswordGenerator>();
builder.Services.AddScoped<IEmailService, EmailService>();

// =========================================================================
// 2.2 FluentValidation - Modern (بدون AspNetCore القديم) 
// =========================================================================
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

// =========================================================================
// 3. Controllers + ValidationFilter + Swagger
// =========================================================================
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>(); //  من Middleware/ValidationFilter.cs
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT Bearer token only",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// =========================================================================
// 4. Authentication & JWT
// =========================================================================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

// =========================================================================
// 5. Authorization Policies
// =========================================================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserRead", p => p.RequireClaim("permission", "USER_READ"));
    options.AddPolicy("UserWrite", p => p.RequireClaim("permission", "USER_WRITE"));
    options.AddPolicy("UserDelete", p => p.RequireClaim("permission", "USER_DELETE"));
    options.AddPolicy("RoleRead", p => p.RequireClaim("permission", "ROLE_READ"));
    options.AddPolicy("RoleWrite", p => p.RequireClaim("permission", "ROLE_WRITE"));
    options.AddPolicy("RoleDelete", p => p.RequireClaim("permission", "ROLE_DELETE"));
    options.AddPolicy("PermissionRead", p => p.RequireClaim("permission", "PERMISSION_READ"));
    options.AddPolicy("PermissionSearch", p => p.RequireClaim("permission", "PERMISSION_SEARCH"));
    options.AddPolicy("PermissionWrite", p => p.RequireClaim("permission", "PERMISSION_WRITE"));
    options.AddPolicy("PermissionUpdate", p => p.RequireClaim("permission", "PERMISSION_UPDATE"));
    options.AddPolicy("PermissionDelete", p => p.RequireClaim("permission", "PERMISSION_DELETE"));
    options.AddPolicy("GroupRead", p => p.RequireClaim("permission", "GROUP_READ"));
    options.AddPolicy("GroupWrite", p => p.RequireClaim("permission", "GROUP_WRITE"));
    options.AddPolicy("GroupDelete", p => p.RequireClaim("permission", "GROUP_DELETE"));
    options.AddPolicy("GroupUpdate", p => p.RequireClaim("permission", "GROUP_UPDATE"));
    options.AddPolicy("ProfileRead", p => p.RequireClaim("permission", "PROFILE_READ"));
    options.AddPolicy("ProfileWrite", p => p.RequireClaim("permission", "PROFILE_WRITE"));
    options.AddPolicy("ProfileDelete", p => p.RequireClaim("permission", "PROFILE_DELETE"));
    options.AddPolicy("AuditRead", p => p.RequireClaim("permission", "AUDIT_READ"));
    options.AddPolicy("AuditArchiveManage", p => p.RequireClaim("permission", "AUDIT_ARCHIVE_MANAGE"));
    options.AddPolicy("SuperAdmin", p => p.RequireRole("SUPER_ADMIN"));
});

// =========================================================================
// 6. Rate Limiting
// =========================================================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueProcessingOrder = QueueProcessingOrder.OldestFirst, QueueLimit = 0 }));
    options.AddPolicy("api", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 100, Window = TimeSpan.FromMinutes(1), QueueProcessingOrder = QueueProcessingOrder.OldestFirst, QueueLimit = 2 }));
    options.AddFixedWindowLimiter("fixed", opt => { opt.PermitLimit = 60; opt.Window = TimeSpan.FromMinutes(1); });
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(new { code = "RATE_LIMIT_EXCEEDED", message = "Too many requests." }, token);
    };
});

// =========================================================================
// 7. CORS
// =========================================================================
builder.Services.AddCors(options => options.AddPolicy("AllowAll", policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// =========================================================================
// 8. Build & Pipeline
// =========================================================================
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity.Api v1"));

app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();