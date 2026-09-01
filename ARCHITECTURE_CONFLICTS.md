# ARCHITECTURE_CONFLICTS.md
## Detected Conflicts Between Tree-SQL-COMPLETE-GOLDEN.txt and 00-START-DTO-Mapping-GOLDEN.md

**Generated:** Phase 1 - Conflict Detection Only (No Code)
**Sources:** Tree-SQL-COMPLETE-GOLDEN.txt (structure + SQL), 00-START-DTO-Mapping-GOLDEN.md (DTO contracts)

---

## DEPENDENCY LAYERING RULE (Authoritative)

```
University.Shared  ←  University.Core  ←  University.Application  ←  University.Infrastructure  ←  University.API
     (DTOs,              (Entities,            (Commands/Queries,         (EF Core,                (Controllers,
      Enums,              Rules,                Behaviors,                 Repositories,             Middleware,
      Common,             Specifications,       Mapping,                   Services,                 Program.cs)
      Constants,          Repository/Service     Validators)               Security,
      Exceptions)         Interfaces)
```

- **University.Shared** = DTOs, Enums, Common (BaseEntity, Result, Error, AuditLogBase), Constants, Exceptions — **ZERO dependencies**
- **University.Core** = Entities (using Shared enums), Rules, Specifications, AND Repository/Service interfaces — depends on Shared only
- **University.Application** = CQRS abstractions, commands/queries, validators, mapping — depends on Core + Shared
- **University.Infrastructure** = EF Core, repository implementations, service implementations — depends on Core + Application
- **ums-wpf** = References Shared only (never Infrastructure)

---

## CONFLICT 1: Circular Dependency — Repository/Service Interfaces in Shared Reference Core Entities

**Severity:** CRITICAL — Breaks acyclic dependency graph

**Tree file defines (lines 146-166):**
```
University.Shared/Interfaces/
  Repositories/
    IGenericRepository.cs        // where T:BaseEntity — OK, uses Shared.BaseEntity
    IUserRepository.cs           // References User entity from Core
    IStudentRepository.cs        // References Student entity from Core
    ICourseRepository.cs         // References Course entity from Core
    IEnrollmentRepository.cs     // References CourseEnrollment entity from Core
    IGradeRepository.cs          // References Grade entity from Core
    IFinancialRepository.cs      // References FinancialRecord entity from Core
    ISemesterRepository.cs       // References Semester entity from Core
    IUnitOfWork.cs               // References Core entities via properties
  Services/
    IAuthService.cs              // Uses Shared DTOs — OK
    IUserService.cs              // Uses Shared DTOs — OK
    IStudentService.cs           // Uses Shared DTOs — OK
    ICourseService.cs            // Uses Shared DTOs — OK
    IEnrollmentService.cs        // Uses Shared DTOs — OK
    IGradeService.cs              // Uses Shared DTOs — OK
    IFinancialService.cs         // Uses Shared DTOs — OK
    IAdministrativeDepartmentService.cs  // Uses Shared DTOs — OK
```

**Problem:** If interfaces live in Shared, Shared must reference Core (for User, Student, Course, etc.). This creates `Shared → Core → Shared` circular dependency.

**Resolution:**
| Interface | Current Location | Required Location | Reason |
|-----------|-----------------|-------------------|--------|
| `IGenericRepository<T>` | Shared | **Shared** (keep) | Generic, only uses Shared.BaseEntity |
| `IUserRepository` | Shared | **Core** | References `User` entity |
| `IStudentRepository` | Shared | **Core** | References `Student` entity |
| `ICourseRepository` | Shared | **Core** | References `Course` entity |
| `IEnrollmentRepository` | Shared | **Core** | References `CourseEnrollment` entity |
| `IGradeRepository` | Shared | **Core** | References `Grade` entity |
| `IFinancialRepository` | Shared | **Core** | References `FinancialRecord` entity |
| `ISemesterRepository` | Shared | **Core** | References `Semester` entity |
| `IUnitOfWork` | Shared | **Core** | References Core entity DbSets |
| `IAuthService` | Shared | **Core** | Uses Shared DTOs but belongs with domain contracts |
| `IUserService` | Shared | **Core** | Same reason |
| `IStudentService` | Shared | **Core** | Same reason |
| `ICourseService` | Shared | **Core** | Same reason |
| `IEnrollmentService` | Shared | **Core** | Same reason |
| `IGradeService` | Shared | **Core** | Same reason |
| `IFinancialService` | Shared | **Core** | Same reason |
| `IAdministrativeDepartmentService` | Shared | **Core** | Same reason |

**Final structure:**
```
University.Shared/Interfaces/
  Repositories/
    IGenericRepository.cs        // where T:BaseEntity — ONLY interface in Shared
    (all others moved to Core)

University.Core/Interfaces/
  Repositories/
    IUserRepository.cs
    IStudentRepository.cs
    ICourseRepository.cs
    IEnrollmentRepository.cs
    IGradeRepository.cs
    IFinancialRepository.cs
    ISemesterRepository.cs
    IUnitOfWork.cs
  Services/
    IAuthService.cs
    IUserService.cs
    IStudentService.cs
    ICourseService.cs
    IEnrollmentService.cs
    IGradeService.cs
    IFinancialService.cs
    IAdministrativeDepartmentService.cs
```

**Dependency chain after fix:** Shared ← Core ← Application ← Infrastructure (acyclic ✓)

---

## CONFLICT 2: Duplicate BaseEntity — Exists in Both Shared and Core

**Severity:** HIGH — Causes ambiguous type resolution

**Tree file defines:**
```
University.Shared/Common/BaseEntity.cs  (line 19)
University.Core/Common/BaseEntity.cs    (line 170-172)
```

Both define identical properties: `Id:Guid`, `CreatedAt:DateTime`, `UpdatedAt:DateTime?`, `IsActive:bool`

**Problem:** Two classes with same name and same properties in different assemblies. Which one does `Student : BaseEntity` inherit from? Which does `IGenericRepository<T> where T : BaseEntity` constrain against?

**Resolution:**
| Item | Action |
|------|--------|
| `University.Core/Common/BaseEntity.cs` | **DELETE** — Core uses `Shared.Common.BaseEntity` via project reference |
| `University.Core/Entities/*.cs` | All entities inherit from `University.Shared.Common.BaseEntity` |
| `University.Shared/Common/BaseEntity.cs` | **KEEP** — Single source of truth |

**Note:** This also applies to `AuditLogBase` (Shared) vs `AuditableLog` (Core) — see Conflict 3.

---

## CONFLICT 3: Duplicate Audit Log Base — AuditLogBase (Shared) vs AuditableLog (Core)

**Severity:** MEDIUM — Same concept, different names, two locations

**Tree file defines:**
```
University.Shared/Common/AuditLogBase.cs  (line 22-23)
  // Id, Timestamp, CreatedAt only for audit_logs

University.Core/Common/AuditableLog.cs    (line 173-174)
  // Id, Timestamp, CreatedAt — same 3 properties
```

**Problem:** Two separate base classes for the same `audit_logs` table concept with different names. `AuditLog` entity (line 204) inherits from `AuditableLog` — unclear relationship to `AuditLogBase`.

**Resolution:**
| Item | Action |
|------|--------|
| `University.Shared/Common/AuditLogBase.cs` | **RENAME** to `AuditLogBase.cs`, keep in Shared |
| `University.Core/Common/AuditableLog.cs` | **DELETE** — `AuditLog` entity inherits from `Shared.Common.AuditLogBase` |
| `University.Core/Common/IAuditable.cs` | **DELETE** — Not needed; audit pattern enforced by base class |

**Result:** Single `AuditLogBase` in Shared. `AuditLog : AuditLogBase` in Core.

---

## CONFLICT 4: DTO Names vs Entity/Table Name Mismatches

**Severity:** MEDIUM — Causes confusion and mapping errors

| Golden Table DTO Name | DB Table | Tree Entity | Mismatch | Resolution |
|----------------------|----------|-------------|----------|------------|
| `CreateAcademicRecordRequestDto` | `academic_records` | `AcademicRecord` | Tree folder called `AcademicStatus` (line 197) | **Rename tree folder** to `AcademicRecords/` to match table and DTO name |
| `CreateRolePermissionRequestDto` | `role_permissions` | `RolePermission` | Tree has `Roles/RolePermission/` (nested under Roles) | **Flatten** — Create `RolePermissions/` as sibling folder, not nested under Roles |
| `CreateCourseSectionRequestDto` | `course_sections` | `CourseSection` | Tree has no `Sections/` DTO folder; properties inline in entity comment (line 192) | **Add** `Sections/CreateCourseSectionRequestDto.cs` to match golden table |
| `CreateInstructorRequestDto` | `instructors` | `Instructor` | Tree has no `Instructors/` DTO folder | **Add** `Instructors/CreateInstructorRequestDto.cs` |
| `CreateClassroomRequestDto` | `classrooms` | `Classroom` | Tree has no `Classrooms/` DTO folder | **Add** `Classrooms/CreateClassroomRequestDto.cs` |
| `CreateSemesterRequestDto` | `semesters` | `Semester` | Tree has no `Semesters/` DTO folder | **Add** `Semesters/CreateSemesterRequestDto.cs` |
| `CreateCoursePrerequisiteRequestDto` | `course_prerequisites` | `CoursePrerequisite` | Tree has no `Prerequisites/` DTO folder | **Add** `Prerequisites/CreateCoursePrerequisiteRequestDto.cs` |
| `CreateMajorRequestDto` | `majors` | `Major` | Tree has no `Majors/` DTO folder | **Add** `Majors/CreateMajorRequestDto.cs` |
| `CreateFacultyRequestDto` | `faculties` | `Faculty` | Tree has no `Faculties/` DTO folder | **Add** `Faculties/CreateFacultyRequestDto.cs` |
| `CreateAcademicDepartmentRequestDto` | `academic_departments` | `AcademicDepartment` | Tree has no `AcademicDepartments/` DTO folder | **Add** `AcademicDepartments/CreateAcademicDepartmentRequestDto.cs` |
| `CreateStudyPlanRequestDto` | `study_plans` | `StudyPlan` | Tree has `StudyPlan` entity (line 193) but no DTO folder | **Add** `StudyPlans/CreateStudyPlanRequestDto.cs` |
| `CreateAttendanceRecordRequestDto` | `attendance_records` | `AttendanceRecord` | Tree has entity (line 195) but no DTO folder | **Add** `AttendanceRecords/CreateAttendanceRecordRequestDto.cs` |
| `CreateGuardianRequestDto` | `guardians` | `Guardian` | Tree has entity (line 203) but no DTO folder | **Add** `Guardians/CreateGuardianRequestDto.cs` |
| `CreateScholarshipRequestDto` | `scholarships` | `Scholarship` | Tree has entity (line 201) but no DTO folder | **Add** `Scholarships/CreateScholarshipRequestDto.cs` |
| `CreateFinancialRefundRequestDto` | `financial_refunds` | `FinancialRefund` | Tree has entity (line 202) but no DTO folder | **Add** `FinancialRefunds/CreateFinancialRefundRequestDto.cs` |
| `CreateSystemNotificationRequestDto` | `system_notifications` | `SystemNotification` | Tree has entity (line 205) but no DTO folder | **Add** `SystemNotifications/CreateSystemNotificationRequestDto.cs` |
| `CreateGraduationRequestDto` | `graduation_requests` | `GraduationRequest` | Tree has entity (line 206) but no DTO folder | **Add** `GraduationRequests/CreateGraduationRequestDto.cs` |

**Summary:** Tree DTO folder structure covers ~13 of 30 tables. Golden table defines Create DTOs for all 30 tables. **17 DTO folders must be added** to match golden table coverage.

---

## CONFLICT 5: Response DTOs in Tree Not Defined in Golden Table

**Severity:** LOW — Golden table only defines Request DTOs; Response DTOs are entity-derived

**Tree defines Response DTOs (lines 36-100):**
| Response DTO | Location | Golden Table Has It? |
|-------------|----------|---------------------|
| `AuthResponseDto` | Auth/ | ✗ (but API contract defines it) |
| `UserResponseDto` | Users/ | ✗ |
| `StudentResponseDto` | Students/ | ✗ |
| `CourseResponseDto` | Courses/ | ✗ |
| `EnrollmentResponseDto` | Enrollments/ | ✗ |
| `GradeResponseDto` | Grades/ | ✗ |
| `FinancialRecordResponseDto` | Finance/ | ✗ |
| `RoleResponseDto` | Roles/ | ✗ |
| `PermissionResponseDto` | Permissions/ | ✗ |
| `AdministrativeDepartmentResponseDto` | AdministrativeDepartments/ | ✗ |
| `PaymentResponseDto` | (referenced in `IFinancialService` line 165) | ✗ — **Not even defined as file** |

**Resolution:** Response DTOs are not in the golden mapping table by design (golden table = DB-to-DTO mapping for creates). They must be defined in Tree/DTO-Mapping as separate output. **No conflict with golden table**, but `PaymentResponseDto` is referenced in service interface without being defined — **add to Tree**.

---

## CONFLICT 6: Duplicate Entity Definition — BaseEntity in Shared vs Core

**Severity:** HIGH — Duplicate type, same file name, different projects

See **Conflict 2** above — resolved by deleting Core's copy.

---

## CONFLICT 7: Extra Enum Without DB Mapping — PaymentStatus

**Severity:** LOW — Unused enum, no CHECK constraint match

**Tree defines (line 116):**
```
University.Shared/Enums/PaymentStatus.cs
  // PENDING, COMPLETED, FAILED, REFUNDED
```

**No DB table has a `payment_status` column.** Payments table has `payment_method` (mapped by `PaymentMethod` enum). No other table uses this enum.

**Resolution:**
| Item | Action |
|------|--------|
| `PaymentStatus.cs` | **DELETE** — Not mapped to any DB column. If payment tracking needed later, add column first. |

---

## CONFLICT 8: UserRole Enum vs Constants Designation

**Severity:** LOW — Mislabeling in tree

**Tree defines (line 126):**
```
University.Shared/Enums/UserRole.cs (Constants)
  // ADMIN, STUDENT, FACULTY, FINANCE_OFFICER
```

Label says `(Constants)` but file is in `Enums/` folder. DB stores role names as `VARCHAR` in `roles` table (not a CHECK constraint — it's a FK to `roles.id`).

**Resolution:**
| Item | Action |
|------|--------|
| `UserRole.cs` | **KEEP** as Enum in `Enums/` — It maps to `roles.role_name` seed values and is used for authorization checks. Remove `(Constants)` label. |

---

## CONFLICT 9: DTO Folder Organization Mismatch — Tree vs Golden Table

**Severity:** LOW — Structural inconsistency

**Tree organizes DTOs by entity subfolder:**
```
DTOs/
  Auth/
  Users/
  Students/
  Courses/
  Enrollments/
  Grades/
  Finance/
  Roles/
  Permissions/
  AdministrativeDepartments/
```

**Golden table lists DTOs flat (no subfolders):**
```
All 30 CreateXxxRequestDto listed as flat table rows
```

**Resolution:** Tree subfolder organization is correct and preferred for maintainability. **Golden table is reference only, not directory structure.** Tree should add missing subfolders (see Conflict 4) following the same pattern.

---

## CONFLICT 10: CreateAuditLogRequestDto — Should Not Exist

**Severity:** MEDIUM — Audit logs are internal, not client-facing DTOs

**Golden table defines (line 62):**
```
CreateAuditLogRequestDto | UserId, Action, Entity, EntityId, TableName, OldValues JSONB, NewValues JSONB, IpAddress, UserAgent | audit_logs
```

**Tree line 204 says:**
```
AuditLog.cs : AuditableLog // SPECIAL - No BaseEntity per Rule 11
```

**Problem:** Audit logs are populated by middleware/infrastructure, never by API clients. Defining a client-facing Create DTO for an internal-only table violates Rule 11 (audit logs are system-generated).

**Resolution:**
| Item | Action |
|------|--------|
| `CreateAuditLogRequestDto` | **DELETE from DTO list** — Audit logs are written by `LoggingBehavior`/`AuditLogService` in Infrastructure, never via API. Internal mapper only. |

---

## CONFLICT 11: Branch/Role DTOs in Golden Table but Missing from Tree Folder

**Severity:** LOW — Minor coverage gap

**Golden table defines:**
- `CreateBranchRequestDto` (line 35)
- `CreateRoleRequestDto` (line 37) — Tree HAS this in `Roles/`
- `CreatePermissionRequestDto` (line 38) — Tree HAS this in `Permissions/`

**Tree missing:**
- `Branches/CreateBranchRequestDto.cs` — Branch CRUD DTO not in Tree DTO folder structure

**Resolution:** Add `Branches/CreateBranchRequestDto.cs` to Tree.

---

## CONFLICT 12: DropCourseRequestDto — In Tree but Not in Golden Table

**Severity:** INFO — No conflict, just asymmetry

**Tree defines (line 67):**
```
Enrollments/DropCourseRequestDto.cs
  // EnrollmentId:Guid, Reason?:string
```

**Golden table does not list this DTO.**

**Resolution:** This is correct — golden table covers Create DTOs only. `DropCourseRequestDto` is a separate action DTO. **Keep in Tree, no conflict.**

---

## SUMMARY TABLE

| # | Conflict | Severity | Resolution Summary |
|---|----------|----------|-------------------|
| 1 | Circular dependency — 17 interfaces in Shared reference Core entities | **CRITICAL** | Move all entity-dependent interfaces to `University.Core/Interfaces/` |
| 2 | Duplicate `BaseEntity` in Shared and Core | **HIGH** | Delete Core copy; Core uses Shared.BaseEntity |
| 3 | `AuditLogBase` (Shared) vs `AuditableLog` (Core) — same concept, different names | **MEDIUM** | Delete Core `AuditableLog` + `IAuditable`; `AuditLog` inherits `Shared.AuditLogBase` |
| 4 | 17 DTO folders missing in Tree (Golden table defines DTOs for all 30 tables) | **MEDIUM** | Add 17 missing DTO folders to Tree |
| 5 | Response DTOs not in Golden table; `PaymentResponseDto` referenced but undefined | **LOW** | Response DTOs are tree-only concern; add missing `PaymentResponseDto` |
| 6 | (Same as Conflict 2) | — | — |
| 7 | `PaymentStatus` enum has no DB column mapping | **LOW** | Delete `PaymentStatus.cs` |
| 8 | `UserRole` labeled `(Constants)` but lives in `Enums/` | **LOW** | Keep as enum, remove label |
| 9 | Tree uses subfolders; Golden table is flat | **LOW** | Tree subfolder structure is correct |
| 10 | `CreateAuditLogRequestDto` should not exist (internal only) | **MEDIUM** | Delete from DTO list |
| 11 | `CreateBranchRequestDto` missing from Tree | **LOW** | Add `Branches/CreateBranchRequestDto.cs` |
| 12 | `DropCourseRequestDto` in Tree but not Golden table | **INFO** | No conflict — asymmetric by design |

---

## IMPLEMENTATION ORDER (When Building)

1. **Fix Conflict 1** (CRITICAL) — Move interfaces to Core before any coding
2. **Fix Conflict 2** (HIGH) — Delete Core BaseEntity, update all `using` statements
3. **Fix Conflict 3** (MEDIUM) — Consolidate audit base classes
4. **Fix Conflict 4** (MEDIUM) — Add 17 missing DTO folders
5. **Fix Conflict 7** (LOW) — Delete PaymentStatus enum
6. **Fix Conflict 10** (MEDIUM) — Remove audit log DTO
7. **Fix Conflicts 5, 8, 9, 11** (LOW) — Cleanup and consistency
