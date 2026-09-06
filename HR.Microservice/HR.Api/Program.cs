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

// Swagger مع زر Authorize
builder.Services.AddSwaggerGen(c => {
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Description = "Paste JWT from ums-api (Bearer token)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// === CORS - هذا هو سبب خطأ 5173 -> 5000 ===
builder.Services.AddCors(o => o.AddPolicy("AllowFrontend", p => 
    p.WithOrigins("http://localhost:5173", "https://localhost:5173")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// === Database ===
var conn = builder.Configuration.GetConnectionString("HrConnection") 
           ?? builder.Configuration.GetConnectionString("HRDatabase")
           ?? "Host=localhost;Port=5432;Database=hr_microservice_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<HrDbContext>(o => o.UseNpgsql(conn));

// === CAP - اجعله اختياري إذا RabbitMQ غير موجود ===
builder.Services.AddCap(o =>
{
    o.UseEntityFramework<HrDbContext>();
    try {
        o.UseRabbitMQ(r => { 
            r.HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost"; 
            r.Port = 5672; r.UserName = "guest"; r.Password = "guest";
            r.ConnectionFactoryOptions = opt => opt.AutomaticRecoveryEnabled = true;
        });
    } catch { /* ignore if RabbitMQ down */ }
    o.FailedRetryCount = 5;
    o.DefaultGroupName = "hr.microservice";
});

// === JWT - يجب أن يكون نفس Key في ums-api ===
var jwtKey = builder.Configuration["JWT:Key"] 
          ?? builder.Configuration["Jwt:Key"] 
          ?? "YOUR_UMS_API_KEY_MUST_MATCH_32_CHARS_MIN"; // ضع هنا نفس Key الموجود في ums-api/appsettings.json

Console.WriteLine($"[HR.Api] Using JWT Key: {jwtKey.Substring(0,10)}...");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(o =>
{
    o.RequireHttpsMetadata = false;
    o.SaveToken = true;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false, // للـ Microservice فصلنا
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "name",
        RoleClaimType = "role"
    };
    // لمعرفة سبب 401
    o.Events = new JwtBearerEvents {
        OnAuthenticationFailed = ctx => {
            Console.WriteLine($"JWT Auth Failed: {ctx.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx => {
            Console.WriteLine($"JWT Validated for: {ctx.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        }
    };
});

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

var app = builder.Build();

app.UseSwagger(); 
app.UseSwaggerUI();

app.UseCors("AllowFrontend"); // <-- مهم جداً قبل Authentication

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

// Migration
using (var scope = app.Services.CreateScope()) { 
    try {
        var db = scope.ServiceProvider.GetRequiredService<HrDbContext>();
        db.Database.Migrate();
        Console.WriteLine("HR DB migrated successfully to hr_microservice_db");
    } catch(Exception ex) {
        Console.WriteLine($"Migration failed: {ex.Message}");
    }
}

app.Run();