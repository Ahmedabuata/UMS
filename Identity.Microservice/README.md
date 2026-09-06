# Identity.Microservice - CLEAN BUILD Phase 1

## الهيكل الصحيح (بعد الحذف)
Identity.Microservice/
├── Identity.Microservice.sln
└── src/
    └── Identity.Api/
        ├── Controllers/AuthController.cs (5 endpoints)
        ├── Validators/GlobalValidators.cs (نفس regex DB)
        ├── Data/IdentityDbContext.cs (HasCheckConstraint مطابق)
        ├── Models/Entities.cs (9 كيانات فقط)
        ├── Services/TokenService.cs (Rotation + Reuse Detection)
        ├── DTOs/AuthDtos.cs
        └── Program.cs

## التشغيل
cd src/Identity.Api
dotnet restore
dotnet ef database update (اذا احتجت)
dotnet run --urls http://localhost:5279

Swagger: http://localhost:5279/swagger/index.html
- POST /api/auth/register
- POST /api/auth/login
- POST /api/auth/refresh-token
- POST /api/auth/revoke-token + POST /api/auth/logout
- GET /api/auth/me

## القيود الثابتة المحترمة
- لا جداول جديدة
- لا حقول جديدة
- chk_users_email, chk_users_username, chk_users_phone, chk_roles_rolename نفس DB
- snake_case DB, PascalCase C#, camelCase React
