using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using Identity.Api.Data;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// 🛑 منع ASP.NET Core من تغيير أسماء الـ Claims مثل permission إلى روابط XML طويلة
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Database Context
builder.Services.AddDbContext<IdentityDbContext>(o => 
    o.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb")));

// Custom Services
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger Documentation with JWT Authentication Support
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT Bearer token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            { 
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                { 
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, 
                    Id = "Bearer" 
                } 
            },
            new string[]{}
        }
    });
});

// Authentication & JWT Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
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

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Users
    options.AddPolicy("UserRead", policy => policy.RequireClaim("permission", "USER_READ"));
    options.AddPolicy("UserWrite", policy => policy.RequireClaim("permission", "USER_WRITE"));
    options.AddPolicy("UserDelete", policy => policy.RequireClaim("permission", "USER_DELETE"));
    
    // Roles
    options.AddPolicy("RoleRead", policy => policy.RequireClaim("permission", "ROLE_READ"));
    options.AddPolicy("RoleWrite", policy => policy.RequireClaim("permission", "ROLE_WRITE"));
    options.AddPolicy("RoleDelete", policy => policy.RequireClaim("permission", "ROLE_DELETE"));
    
    // Permissions
    options.AddPolicy("PermissionRead", policy => policy.RequireClaim("permission", "PERMISSION_READ"));
    options.AddPolicy("PermissionWrite", policy => policy.RequireClaim("permission", "PERMISSION_WRITE"));
    
    // Groups
    options.AddPolicy("GroupRead", policy => policy.RequireClaim("permission", "GROUP_READ"));
    options.AddPolicy("GroupWrite", policy => policy.RequireClaim("permission", "GROUP_WRITE"));
    options.AddPolicy("GroupDelete", policy => policy.RequireClaim("permission", "GROUP_DELETE"));
    
    // AuditLogs
    options.AddPolicy("AuditRead", policy => policy.RequireClaim("permission", "AUDIT_READ"));
    
    // Super Admin - يملك كل الصلاحيات
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SUPER_ADMIN"));
});

// Rate Limiting Policy
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));

    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Too many requests. Please try again later." }, token);
    };
});

// CORS Configuration
builder.Services.AddCors(p => p.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Configure Middleware Pipeline
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity.Api v1"));

app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();