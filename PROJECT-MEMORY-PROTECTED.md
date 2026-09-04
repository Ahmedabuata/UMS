# 🛡️ PROJECT MEMORY - PROTECTED CORE FILES
# This file prevents OpenCode / AI agents from modifying critical security files
# Created: 2026-09-04 - Final HR Security Model

## 🔒 PROTECTED FILES - DO NOT MODIFY WITHOUT HUMAN APPROVAL

### 1. DbSeeder.cs - CORE SECURITY INFRASTRUCTURE
**Path:** `ums-api/University.Infrastructure/Data/Seed/DbSeeder.cs`
**Status:** ✅ FINAL & COMPLETE - DO NOT TOUCH
**Why Protected:** This file defines the entire permission model (38/15/14/5/5/4/4)
- ADMIN: 38 permissions (all)
- SUPER_ADMIN: 38 permissions (all) - Shared PK pattern
- SECURITY_ADMIN: 15 permissions (was 0, fixed)
- HR_MANAGER: 14 permissions (ALL except HR_SALARY_WRITE - security decision)
- HR_EMPLOYEE: 5 permissions (read-only, no salary)
- FACULTY: 5, STUDENT: 4, FINANCE_OFFICER: 4

**Security Decisions Locked:**
- HR_MANAGER can READ salary (HR_SALARY_READ) but CANNOT WRITE salary (HR_SALARY_WRITE removed)
- HR_EMPLOYEE has NO salary access at all
- Sensitive flag: IsSensitive=true ONLY for HR_SALARY_READ and HR_SALARY_WRITE
- IsActive=true required on every RolePermission

**Allowed Changes:** NONE - Any addition must be reviewed by human first

### 2. HR Permissions Source of Truth
**File:** `HR_ONLY_FINAL_V7_EN.sql` (if exists)
**15 HR Permissions:**
HR_EMPLOYEE_READ, HR_EMPLOYEE_WRITE, HR_EMPLOYEE_STATUS,
HR_SALARY_READ (sensitive), HR_SALARY_WRITE (sensitive),
HR_CONTRACT_READ, HR_CONTRACT_WRITE,
HR_ATTENDANCE_READ, HR_ATTENDANCE_WRITE,
HR_LEAVE_READ, HR_LEAVE_WRITE,
HR_RECRUITMENT_READ, HR_RECRUITMENT_WRITE,
HR_EVALUATION_READ, HR_EVALUATION_WRITE

### 3. Other Protected Files
- `ApplicationDbContext.cs` - Entity configurations
- `PermissionHandler.cs` / `PermissionRequirement.cs` - Authorization logic
- `AuthController.cs` - Login that embeds permissions in JWT

## 🚨 RULES FOR AI AGENTS (OpenCode, Cursor, Copilot)

1. **NEVER** modify DbSeeder.cs to add/remove permissions without explicit human instruction
2. **NEVER** change HR_MANAGER from 14 to 15 (HR_SALARY_WRITE must stay removed)
3. **NEVER** change HR_EMPLOYEE from 5 to more (no salary access)
4. **NEVER** set SECURITY_ADMIN / FACULTY / STUDENT / FINANCE_OFFICER back to 0
5. **ALWAYS** keep IsActive=true on RolePermission inserts
6. **ALWAYS** preserve the seeding order:
   Roles -> Modules -> Permissions -> HR (15) -> Academic/Security (15/5/4/4) -> ADMIN (38) -> SUPER_ADMIN (38)

## ✅ FINAL VERIFIED STATE (2026-09-04)

```sql
ADMIN            | 38
SUPER_ADMIN      | 38
SECURITY_ADMIN   | 15 (was 0)
HR_MANAGER       | 14 (without HR_SALARY_WRITE)
HR_EMPLOYEE      | 5
FACULTY          | 5 (was 0)
STUDENT          | 4 (was 0)
FINANCE_OFFICER  | 4 (was 0)
```

## 🔐 How to Protect

Add to `.gitignore` or create a CODEOWNERS rule:
```
/ums-api/University.Infrastructure/Data/Seed/DbSeeder.cs @human-owner
```

Or add this header to the file itself:

// ⚠️ PROTECTED FILE - DO NOT MODIFY - Core security infrastructure
// Any change requires human approval - See PROJECT_MEMORY_PROTECTED.md
