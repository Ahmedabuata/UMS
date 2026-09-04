##################################################
# MEMORY_ALL.md - Consolidated Memory & Rules
# Extraction date: 2026-09-03
# Host: Linux (/workspace) - mapped from C:\Projects\UMS
##################################################

==================================================
NOTE: Standard files NOT found (no match):
  MEMORY.md, AGENT.md, AGENTS.md, CLAUDE.md, GEMINI.md,
  CONTEXT.md, RULES.md, PROJECT_RULES.md, *.mdc,
  .cursorrules, opencode.json, .cursor/.opencode/.agent/docs dirs
==================================================

Consolidated from the two existing memory-context files:
  1) PROJECT-MEMORY-FINAL-SINGLE.md
  2) 00-START-DTO-Mapping-GOLDEN.md


##################################################
# FILE 1: PROJECT-MEMORY-FINAL-SINGLE.md
##################################################
# UMS_Project - الذاكرة المرجعية الموحدة النهائية - النسخة الوحيدة الصحيحة
# تاريخ الدمج النهائي: 2026-09-02 - 31 جدول
# هذه هي النسخة الوحيدة التي يجب الاحتفاظ بها - احذف باقي الملفات
# يتم قراءة هذا الملف تلقائيا بواسطة OpenCode AI و Meta AI

## 1. معلومات المشروع الأساسية
- المسار المحلي: C:\Users\AAlma\Desktop\UMS_Project
- النوع: University Management System (UMS) - Greenfield
- حاوية قاعدة البيانات: ums-db -> postgres:16-alpine - المنفذ 5432
- اسم قاعدة البيانات: ums_db - المستخدم: postgres / postgres123
- حاوية الذكاء الاصطناعي: opencode -> http://localhost:4096 عبر npx opencode-ai serve
- Frontend: http://localhost:5173 (Vite / React) - Backend API: http://localhost:3000/api أو 8080/api
- مجلد التهيئة: ./init-scripts/01-init.sql هو المصدر الوحيد للحقيقة (Single Source of Truth)

## 2. هيكل قاعدة البيانات - 31 جدول مجمد (نهائي بعد التصحيح)
### الجداول الأساسية من 01-init.sql (30 جدول):
branches, administrative_departments, roles, permissions, role_permissions, faculties, academic_departments, majors, courses, course_prerequisites, semesters, classrooms, users, instructors, students, course_sections, study_plans, course_enrollments, attendance_records, grades, academic_records, tuition_fees, financial_records, payments, scholarships, financial_refunds, guardians, audit_logs, system_notifications, graduation_requests

### الجدول 31 المضاف - تصحيح معماري حرج 2026-09-02 - تم تنفيذه:
employees (id UUID PK, employee_number VARCHAR(50) UNIQUE, full_name VARCHAR(100), email UNIQUE, phone, department_id FK->administrative_departments, branch_id FK->branches, contract_type [Full-time, Part-time, Contract], status [Active, Inactive, On Leave], hire_date DATE, user_id FK->users NULL, created_at, updated_at)
- السبب: لا يمكن انشاء محاضر بدون وجود جدول موظفين لأن المحاضر هو موظف، وكذلك مدير البيانات والأمن والمالية والشؤون كلهم موظفون. instructors كان موجود كجدول منفصل لكنه نوع من الموظف.
- العلاقة الجديدة النهائية:
  employees (Master) 1--1 instructors عبر instructors.employee_id FK->employees.id
  employees 1--1 users عبر employees.user_id FK->users.id (الربط يكون من employees إلى users، وايضا من students/instructors إلى users عبر user_id nullable)
  employees N--1 administrative_departments
  employees N--1 branches
- instructors بعد التصحيح: (id, employee_id FK->employees, academic_title [محاضر, أستاذ مساعد, أستاذ مشارك, أستاذ دكتور], faculty_id, major_id, specialization, user_id nullable)

### administrative_departments - تفاصيل:
(id, branch_id FK, department_name, department_code UNIQUE, description, is_active BOOLEAN DEFAULT TRUE, created_at, updated_at)
- السجلات: Student Affairs (STD-AFF), Finance & Accounting (FIN-DEPT), HR (HR-DEPT), Procurement & Logistics (PROC-LOG) - كان Active ثم حول الى Inactive واختفى بسبب Bug الفلتر - تم اصلاحه
- الحالة: is_active BOOLEAN وليس enum Status

### users - الحقيقة من DB-FINAL-CORRECT.sql:
CREATE TABLE users (id UUID PK, username UNIQUE, email UNIQUE, password_hash, full_name, phone_number, branch_id FK, role_id FK->roles, last_login, is_active BOOLEAN, created_at, updated_at)
- لا يوجد linked_id ولا linked_type في users - الربط يكون من الجهة المقابلة: students.user_id FK و instructors.user_id FK و employees.user_id FK (nullable)
- تم تحويل Student.UserId و Instructor.UserId إلى nullable في ميجريشن MakeStudentInstructorUserLinkOptional + OnDelete(SetNull)

## 3. المشاكل الحرجة - تم حلها - ابقيها كذاكرة فقط (لا تعيد تنفيذها)

### مشكلة A: ربط المستخدم برقمه (Security Manager > Users) - تم حلها
- الوضع السابق الخاطئ: فورم Create User يسمح بادخال Username, Email, Password, Full name, Phone, User type, Roles كـ checkboxes بدون تحقق - خطر انشاء طالب وهمي.
- الحل المنفذ والمثبت:
  1. ترتيب الفورم: User type أولا -> حقل التحقق (Student Number / Employee Number) -> زر Verify
  2. Endpoint جديد تم انشاؤه: GET /api/security/users/validate-identifier?type=...&number=...
  3. اذا الرقم موجود: تعبئة تلقائية لـ full_name, email, phone (read-only) وتعيين Role تلقائي
  4. اذا الرقم غير موجود: رسالة خطأ وزر Create معطل
  5. الربط: بعد انشاء user يتم ربط students.user_id = user.Id أو instructors.user_id أو employees.user_id
  6. Backend Validation إلزامي حتى لو تم تجاوز Frontend
- الملفات المعدلة: Users.jsx (اعادة تصميم مودال خطوات Type->Verify->Auto-fill->Roles), client.js (اضافة validateIdentifier), SecurityUsersController.cs (اضافة endpoint validate-identifier), SecurityUserService.cs (ValidateIdentifierAsync + تعديل CreateAsync), SecurityDtos.cs (اضافة IdentifierNumber)
- السجلات التجريبية: طلاب 20210001, 20220007 ومحاضرين INS-001, INS-002 - جميعها user_id=NULL قابلة للربط

### مشكلة B: اختفاء الأقسام عند تعطيلها (Administrative Departments) - تم حلها
- الوضع السابق: GetAllAsync يفلتر .Where(d => d.IsActive) -> يرجع active فقط - القائمة تظهر ACTIVE فقط - Delete يعمل Soft Delete is_active=false فيختفي القسم
- الحل المنفذ والمثبت:
  1. Backend: GetAllAsync يرجع الكل دائما بدون فلتر، مرتب Active أولا ثم بالاسم: OrderByDescending(d => d.IsActive).ThenBy(d => d.DepartmentName) - GetByIdAsync لا يفلتر ايضا
  2. Controller: GET /api/AdministrativeDepartments يرجع الكل - اضافة PUT /{id}/restore يعيد IsActive=true
  3. Service: RestoreAsync(id) يضع IsActive=true
  4. Frontend: loadDepartments يرجع الكل - Total = departments.length - STATUS يظهر Active اخضر و Inactive رمادي - ACTIONS: Edit + Deactivate (اذا active) أو Restore (اذا inactive)
  5. API: adminDepartmentsApi.restore(id) => PUT /AdministrativeDepartments/{id}/restore
  6. SQL فوري: UPDATE administrative_departments SET is_active=true WHERE department_code='PROC-LOG'
- التحقق: يجب ان ترى 4 اقسام دائما - Total يبقى 4 - Deactivate يبقيه في الجدول Inactive - Restore يعيده Active

## 4. API Endpoints - سجل حي من OpenCode (لا تعيد انشاءها - للمرجع فقط)

### Auth /api/auth: POST login, register, refresh (AllowAnonymous), change-password
### AdministrativeDepartments /api/AdministrativeDepartments: GET (all), GET {id}, POST, PUT {id}, DELETE {id} (soft), PUT {id}/restore
### AcademicDepartments /api/AcademicDepartments: GET ?facultyId, GET {id}, POST, PUT {id}, DELETE {id}
### Branches /api/Branches: GET, GET {id}, POST, PUT {id}, DELETE {id}
### Buildings /api/Buildings: GET, GET {id}, GET {id}/classrooms, POST, PUT {id}, DELETE {id}
### Classrooms /api/Classrooms: GET {id}, POST, PUT {id}, DELETE {id}
### Courses /api/Courses: GET, GET {id}, GET {id}/prerequisites, POST, PUT {id}, DELETE {id}
### Enrollments /api/Enrollments: POST, POST {id}/drop, GET student/{studentId}/semester/{semesterId}, GET student/{studentId}/course/{courseId}/check-prerequisites
### Faculties /api/Faculties: GET, GET {id}, GET {facultyId}/academic-departments, POST, PUT {id}, DELETE {id}
### Finance /api/Finance: GET balance/{studentId}, GET {id}, POST payments, POST students/{studentId}/scholarships/{scholarshipId}, POST {id}/refund
### Grades /api/Grades: POST, PUT {id}, POST {id}/lock, GET student/{studentId}, GET student/{studentId}/gpa
### Semesters /api/Semesters: GET, GET {id}, POST, PUT {id}, PUT {id}/open, {id}/close, {id}/set-current, DELETE {id}
### Students /api/Students: GET, GET {id}, POST, PUT {id}, GET {id}/gpa
### Security Users /api/security/users: GET ?search=&branchCode=&userType=&active=&page=&pageSize=, GET {id}, GET validate-identifier?type=&number=, POST, PUT {id}, DELETE {id}, PUT {id}/activate, {id}/unlock, {id}/reset-password, {id}/roles, GET {id}/roles, {id}/groups, POST {id}/groups/{groupId}, DELETE {id}/groups/{groupId}
### Security Roles /api/security/roles: GET ?branchCode=, GET {id}, POST, PUT {id}, DELETE {id}, GET {id}/permissions, PUT {id}/permissions
### Role Permissions /api/security/roles/{roleId}/permissions: POST {permissionId}, DELETE {permissionId}
### Security Groups /api/security/groups: GET ?branchCode=, GET {id}, POST, PUT {id}, DELETE {id}, GET {id}/members, POST {id}/members/{userId}, DELETE {id}/members/{userId}
### Security Permissions /api/security/permissions: GET ?module=, GET grouped, GET modules
### Security Policies /api/security/policies: GET, PUT
### Audit Logs /api/security/audit-logs: GET ?search=&action=&entity=&from=&to=&page=&pageSize=, POST
### Employees (جديد 31) /api/employees: GET ?department=&branch=&contractType=&status=&search=, GET {id}, POST, PUT {id}, DELETE {id} (soft)

## 5. Frontend Routes - من App.jsx - 14 صفحة
Path /dashboard -> Dashboard.jsx, /users -> Users.jsx (USER_READ), /roles -> Roles.jsx, /groups -> Groups.jsx, /permissions -> Permissions.jsx, /audit-logs -> AuditLogs.jsx, /security-policies -> SecurityPolicies.jsx, /branches -> Branches.jsx, /modules -> Modules.jsx, /administrative-departments -> AdministrativeDepartments.jsx, /faculties -> Faculties.jsx, /buildings -> Buildings.jsx, /semesters -> Semesters.jsx, /login -> Login.jsx
Menu: Infrastructure Management (Branches, Modules, Administrative Departments) + Security + Faculties + Buildings + Semesters + HR Module (جديد)

## 6. ARCHITECTURE CONFLICTS - تم توثيقها وحلها - كذاكرة فقط

CONFLICT 1: BaseEntity - audit_logs ليس BaseEntity - هو منفصل بدون UpdatedAt/IsActive - 29 جدول الأخرى BaseEntity {Id, CreatedAt, UpdatedAt, IsActive}
CONFLICT 2: attendance_records كان بدون is_active - تمت اضافته
CONFLICT 3: EnrollmentStatus 5 قيم: ENROLLED, DROPPED, WITHDRAWN, COMPLETED, FAILED
CONFLICT 4: StudentStatus (ST_ACTIVE, ST_INACTIVE, ST_GRADUATED, ST_SUSPENDED) منفصل عن AcademicStanding (GOOD_STANDING, PROBATION, SUSPENDED, HONORS, DEANS_LIST)
CONFLICT 5: Permission يجب ان يحتوي Module
CONFLICT 6: branches.branch_code موجود
CONFLICT 7: PaymentMethod 4 قيم: CASH, CARD, BANK_TRANSFER, ONLINE
CONFLICT 8: Grade IsActive تقني فقط - لا تستخدم soft delete - استخدم IsLocked
CONFLICT 9: DTO Password -> Entity PasswordHash
CONFLICT 10: IGenericRepository - AddRangeAsync, UpdateAsync, SaveChangesAsync من Full-Tree
CONFLICT 11: Table snake_case plural -> Entity PascalCase singular
CONFLICT 12: FinancialRecordStatus {ACTIVE, CLOSED, OVERDUE} منفصل عن PaymentStatus {PENDING, COMPLETED, FAILED, REFUNDED} و RefundStatus {PENDING, APPROVED, REJECTED}
CONFLICT Circular Dependency: IGenericRepository فقط في Shared - باقي Repositories و Services Interfaces في Core - Shared <- Core <- Application <- Infrastructure

## 7. قواعد العمل مع OpenCode AI - نهائية
- تمت الموافقة على الجدول 31 employees - لا يتم انشاء جدول جديد خارج الـ 31 جدول + administrative_departments الا بعد موافقة جديدة
- كل تغيير في قاعدة البيانات يجب ان يمر عبر init-scripts/ (01-init.sql + 02-create-employees.sql + 03-fix-admin-departments.sql)
- كلمة مرور opencode تستخدم فقط عند النشر على الانترنت، اما على localhost فبدون باسورد
- عند تنفيذ docker compose down استخدم -v فقط عند الحاجة لاعادة تهيئة كاملة
- ملف .env لا يجب ان يحتوي OPENCODE_SERVER_PASSWORD للتجربة المحلية
- لا تستخدم PATCH method
- الميغريشنات اسماء FK الفعلية مثل students_user_id_fkey وليست FK_students_users_user_id
- audit_logs immutable

## 8. Module 1 HR - القاعدة الذهبية - الهيكلية العامة لشاشات الموارد البشرية
"ابني فقط التبويبات التي تتوافق مع قاعدة البيانات الفعلية. أما التبويبات التي لا يوجد لها جدول فابنِ تبويبات التصميم بدون ربط مع قاعدة البيانات لا حقيقة ولا وهمية وتبقى في حالة غير مفعلة كتطوير مستقبلي في حال لم تكن في قاعدة البيانات"

التطبيق النهائي:
- دليل الموظفين (Employees Directory): ACTIVE ومربوط بجدول employees الحقيقي (Master) مع JOIN instructors (تفصيل أكاديمي) و users (حساب الدخول) و administrative_departments و branches - مع فلترة حسب الإدارة والفرع ونوع العقد والحالة - بطاقة تفصيلية Personal Info, Academic Title, Contact, Linked User Account
- إدارة العقود والرواتب (Contracts & Payroll): INACTIVE تصميم فقط UI مع Lock + badge Coming Soon + رسالة "لا يوجد جدول contracts/payroll في قاعدة البيانات الحالية (31 جدول) - تطوير مستقبلي" - لا API ولا mock data
- إدارة الإجازات والغياب (Leaves & Attendance): INACTIVE تصميم فقط - رسالة "attendance_records الحالي للطلاب فقط (course_enrollments) - يتطلب جدول employee_leaves مستقبلي"
- الترقيات والتقييم الأكاديمي (Appraisals & Academic Titles): PARTIALLY ACTIVE - عرض وفلترة الرتب من instructors.academic_title (محاضر، أستاذ مساعد، أستاذ مشارك، أستاذ دكتور) مع JOIN employees - Timeline الترقية Coming Soon

السبب المعماري: المحاضر هو موظف، ومدير البيانات والأمن والمالية والشؤون كلهم موظفون - لذا employees هو الجدول الأم

## 9. ما يجب تنفيذه الآن - فقط ما لم ينفذ
- التأكد من 02-create-employees.sql منفذ في Live DB
- بناء HRModule.jsx حسب القاعدة الذهبية (تبويب واحد ACTIVE + 3 INACTIVE)
- اختبار Users Verification (رقم 20210001 موجود -> auto-fill، رقم وهمي -> رفض)
- اختبار Administrative Departments Always Visible (4 اقسام دائما، Deactivate يبقيه Inactive، Restore يعيده)

## 10. ما تم تنفيذه - كذاكرة فقط - لا تعيد تنفيذه
- PATCH-ADMIN-DEPARTMENTS-ALWAYS-VISIBLE: Backend GetAll بدون فلتر + Restore endpoint + Frontend Status badges + Total 4 دائما
- PATCH-USERS-VERIFICATION: ValidateIdentifier endpoint + Users.jsx خطوات Type->Verify->Auto-fill->Roles + CreateAsync يربط Student.UserId/Instructor.UserId/Employee.UserId
- انشاء employees كجدول 31

تاريخ آخر تحديث: 2026-09-02 - النسخة النهائية الموحدة - بواسطة Meta AI بالتعاون مع Ahmed - هذه هي النسخة الوحيدة الصحيحة احذف باقي الملفات


##################################################
# FILE 2: 00-START-DTO-Mapping-GOLDEN.md
##################################################
# DTO-Mapping GOLDEN START - SOURCE OF TRUTH - FROM SCRATCH
# No C# code yet - Only canonical DTO definitions

## ABSOLUTE RULE
Database column is canonical string. DTO property is same semantic name.
C#: PascalCase, TypeScript: camelCase, DB: snake_case, JSON: camelCase
Example: course_enrollments.student_id -> C# StudentId -> TS studentId -> JSON studentId

## CORE CONTRACTS - MUST BE PRESERVED

### EnrollRequestDto (MOST IMPORTANT - Rule 8)
- **C#:** StudentId:Guid, SectionId:Guid, SemesterId:Guid
- **TS:** studentId:string, sectionId:string, semesterId:string
- **JSON:** {"studentId":"uuid","sectionId":"uuid","semesterId":"uuid"}
- **DB:** course_enrollments.student_id UUID FK students.id, section_id UUID FK course_sections.id, semester_id UUID FK semesters.id
- **Constraint:** UNIQUE(student_id, section_id, semester_id)
- **API:** POST /api/enrollments

### PaymentRequestDto
- **C#:** FinancialId:Guid, Amount:decimal>0, PaymentMethod:PaymentMethod(CASH/CARD/BANK_TRANSFER/ONLINE), TransactionId?:string
- **TS:** financialId:string, amount:number, paymentMethod:'CASH'|'CARD'|'BANK_TRANSFER'|'ONLINE', transactionId?:string
- **DB:** payments.financial_id, amount CHECK >0, payment_method CHECK CASH/CARD/BANK_TRANSFER/ONLINE, transaction_id UNIQUE
- **API:** POST /api/finance/payments

### CreateAdministrativeDepartmentRequestDto
- **C#:** BranchId:Guid, DepartmentName:string, DepartmentCode:string, Description?:string
- **TS:** branchId:string, departmentName:string, departmentCode:string, description?:string
- **DB:** administrative_departments.branch_id, department_name, department_code UNIQUE, description
- **API:** POST /api/administrative-departments

### All DTOs Mapping Table (30 tables)

| DTO (Canonical Name) | Properties | DB Table | DB Columns |
|----------------------|------------|----------|------------|
| CreateBranchRequestDto | BranchName, BranchCode | branches | branch_name, branch_code |
| CreateAdministrativeDepartmentRequestDto | BranchId, DepartmentName, DepartmentCode, Description | administrative_departments | branch_id, department_name, department_code, description |
| CreateRoleRequestDto | RoleName, Description | roles | role_name, description |
| CreatePermissionRequestDto | PermissionName, Description, Module | permissions | permission_name, description, module |
| CreateRolePermissionRequestDto | RoleId, PermissionId | role_permissions | role_id, permission_id |
| CreateFacultyRequestDto | BranchId, FacultyName, FacultyCode, DeanName | faculties | branch_id, faculty_name, faculty_code, dean_name |
| CreateAcademicDepartmentRequestDto | FacultyId, DepartmentName, DepartmentCode, HeadName | academic_departments | faculty_id, department_name, department_code, head_name |
| CreateMajorRequestDto | DepartmentId, MajorName, MajorCode, TotalCreditHours | majors | department_id, major_name, major_code, total_credit_hours |
| CreateCourseRequestDto | CourseCode, CourseName, Description, CreditHours 1-6, LectureHours, LabHours, MaxStudents | courses | course_code, course_name, description, credit_hours, lecture_hours, lab_hours, max_students |
| CreateCoursePrerequisiteRequestDto | CourseId, PrerequisiteCourseId, IsMandatory | course_prerequisites | course_id, prerequisite_course_id, is_mandatory |
| CreateSemesterRequestDto | SemesterName, SemesterCode, AcademicYear, StartDate, EndDate, IsCurrent | semesters | semester_name, semester_code, academic_year, start_date, end_date, is_current |
| CreateClassroomRequestDto | BranchId, RoomNumber, BuildingName, Capacity, RoomType | classrooms | branch_id, room_number, building_name, capacity, room_type |
| CreateUserRequestDto | Username, Email, Password (plain -> hashed), FullName, PhoneNumber, BranchId, RoleId | users | username, email, password_hash, full_name, phone_number, branch_id, role_id |
| CreateInstructorRequestDto | UserId, FacultyId, InstructorNumber, AcademicRank, Specialization | instructors | user_id, faculty_id, instructor_number, academic_rank, specialization |
| CreateStudentRequestDto | UserId, MajorId, StudentNumber, EnrollmentDate, Status | students | user_id, major_id, student_number, enrollment_date, status |
| CreateCourseSectionRequestDto | CourseId, SemesterId, ClassroomId, InstructorId, SectionNumber, MaxCapacity, CurrentEnrollment, ScheduleDays, StartTime, EndTime | course_sections | course_id, semester_id, classroom_id, instructor_id, section_number, max_capacity, current_enrollment, schedule_days, start_time, end_time |
| CreateStudyPlanRequestDto | MajorId, CourseId, SemesterNumber 1-12, IsMandatory | study_plans | major_id, course_id, semester_number, is_mandatory |
| EnrollRequestDto | StudentId, SectionId, SemesterId | course_enrollments | student_id, section_id, semester_id |
| CreateAttendanceRecordRequestDto | EnrollmentId, AttendanceDate, IsPresent, Notes | attendance_records | enrollment_id, attendance_date, is_present, notes |
| SubmitGradeRequestDto | EnrollmentId, MidtermScore 0-100, FinalScore 0-100 | grades | enrollment_id, midterm_score, final_score |
| CreateAcademicRecordRequestDto | StudentId, SemesterId, SemesterGpa 0-4, CumulativeGpa 0-4, TotalCredits, TotalPoints, AcademicStatus | academic_records | student_id, semester_id, semester_gpa, cumulative_gpa, total_credits, total_points, academic_status |
| CreateTuitionFeeRequestDto | MajorId, AcademicYear, CreditHourPrice | tuition_fees | major_id, academic_year, credit_hour_price |
| CreateFinancialRecordRequestDto | StudentId, SemesterId, TotalDue, TotalPaid, Balance, Status | financial_records | student_id, semester_id, total_due, total_paid, balance, status |
| PaymentRequestDto | FinancialId, Amount>0, PaymentMethod, TransactionId, Notes | payments | financial_id, amount, payment_method, transaction_id, notes |
| CreateScholarshipRequestDto | ScholarshipName, ScholarshipCode, DiscountPercentage 0-100, MaxAmount | scholarships | scholarship_name, scholarship_code, discount_percentage, max_amount |
| CreateFinancialRefundRequestDto | FinancialId, Amount>0, Reason, Status | financial_refunds | financial_id, amount, reason, status |
| CreateGuardianRequestDto | StudentId, FullName, Phone, Email, Relationship, IsPrimary | guardians | student_id, full_name, phone, email, relationship, is_primary |
| CreateAuditLogRequestDto | UserId, Action, Entity, EntityId, TableName, OldValues JSONB, NewValues JSONB, IpAddress, UserAgent | audit_logs | user_id, action, entity, entity_id, table_name, old_values, new_values, ip_address, user_agent, timestamp, created_at - SPECIAL no is_active/updated_at |
| CreateSystemNotificationRequestDto | UserId, Title, Message, NotificationType, IsRead | system_notifications | user_id, title, message, notification_type, is_read |
| CreateGraduationRequestDto | StudentId, ExpectedGraduationDate, Status, ClearanceStatus, GpaAtRequest 0-4, TotalCreditsAtRequest, Notes | graduation_requests | student_id, expected_graduation_date, status, clearance_status, gpa_at_request, total_credits_at_request, notes |

## ENUM RULE - CRITICAL

Numeric values (1,2,3...) are internal only.
DB column VARCHAR stores canonical string: 'ENROLLED' not '1', 'CASH' not '1'
Mapping: C# ENROLLED=1 -> DB 'ENROLLED'
EF Core: HasConversion(v=>v.ToString(), v=>Enum.Parse)
Never infer DB values, use exact canonical strings from CHECK constraints.

## API CONTRACTS

POST /api/auth/login {Email, Password} -> Token
POST /api/auth/register {Username, Email, Password, FullName, RoleId} -> Token
POST /api/enrollments {StudentId, SectionId, SemesterId} -> 201 Created
GET /api/enrollments/student/{studentId}
POST /api/grades {EnrollmentId, MidtermScore, FinalScore}
GET /api/grades/student/{studentId}
POST /api/finance/payments {FinancialId, Amount, PaymentMethod}

All clients (Web Next.js, Mobile, WPF) use same API, same DTOs, same JWT. No direct DB access.

## VALIDATION

GPA 0-4, Scores 0-100, CreditHours 1-6, Amount >0, Discount 0-100, All IDs required where FK
Server validation authoritative.
