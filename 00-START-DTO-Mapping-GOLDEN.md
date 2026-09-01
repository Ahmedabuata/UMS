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
