-- UMS 30 Tables - FINAL CORRECTED - Per ARCHITECTURE_CONFLICTS.md
-- PostgreSQL 15 - BaseEntity applies to 29 tables, audit_logs separate (Rule 11)
-- Canonical per Rule 2
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

DROP TABLE IF EXISTS audit_logs, system_notifications, graduation_requests, guardians, financial_refunds, payments, scholarships, financial_records, tuition_fees, academic_records, grades, attendance_records, course_enrollments, study_plans, course_prerequisites, course_sections, students, instructors, users, classrooms, semesters, courses, majors, academic_departments, faculties, administrative_departments, role_permissions, permissions, roles, branches CASCADE;

-- 1. branches
CREATE TABLE branches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_name VARCHAR(100) NOT NULL,
    branch_code VARCHAR(20) UNIQUE NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. administrative_departments
CREATE TABLE administrative_departments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id UUID NOT NULL REFERENCES branches(id) ON DELETE RESTRICT,
    department_name VARCHAR(100) NOT NULL,
    department_code VARCHAR(20) UNIQUE NOT NULL,
    description VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 3. roles
CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name VARCHAR(50) UNIQUE NOT NULL,
    description VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 4. permissions (Module included - Conflict 5 resolved)
CREATE TABLE permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    permission_name VARCHAR(100) UNIQUE NOT NULL,
    description VARCHAR(255),
    module VARCHAR(50) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 5. role_permissions
CREATE TABLE role_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(role_id, permission_id)
);

-- 6. faculties
CREATE TABLE faculties (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id UUID NOT NULL REFERENCES branches(id) ON DELETE RESTRICT,
    faculty_name VARCHAR(100) NOT NULL,
    faculty_code VARCHAR(20) UNIQUE,
    dean_name VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 7. academic_departments
CREATE TABLE academic_departments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    faculty_id UUID NOT NULL REFERENCES faculties(id) ON DELETE RESTRICT,
    department_name VARCHAR(100) NOT NULL,
    department_code VARCHAR(20) UNIQUE,
    head_name VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 8. majors
CREATE TABLE majors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    department_id UUID NOT NULL REFERENCES academic_departments(id) ON DELETE RESTRICT,
    major_name VARCHAR(100) NOT NULL,
    major_code VARCHAR(20) UNIQUE,
    total_credit_hours INT DEFAULT 130,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 9. courses
CREATE TABLE courses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    course_code VARCHAR(20) UNIQUE NOT NULL,
    course_name VARCHAR(150) NOT NULL,
    description TEXT,
    credit_hours INT CHECK (credit_hours BETWEEN 1 AND 6) NOT NULL,
    lecture_hours INT DEFAULT 3,
    lab_hours INT DEFAULT 0,
    max_students INT DEFAULT 40,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 10. course_prerequisites
CREATE TABLE course_prerequisites (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    prerequisite_course_id UUID NOT NULL REFERENCES courses(id) ON DELETE RESTRICT,
    is_mandatory BOOLEAN DEFAULT TRUE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(course_id, prerequisite_course_id)
);

-- 11. semesters
CREATE TABLE semesters (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    semester_name VARCHAR(50) NOT NULL,
    semester_code VARCHAR(20) UNIQUE NOT NULL,
    academic_year VARCHAR(9) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    is_current BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 12. classrooms
CREATE TABLE classrooms (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id UUID NOT NULL REFERENCES branches(id) ON DELETE RESTRICT,
    room_number VARCHAR(20) NOT NULL,
    building_name VARCHAR(50),
    capacity INT NOT NULL DEFAULT 30,
    room_type VARCHAR(20) DEFAULT 'LECTURE' CHECK (room_type IN ('LECTURE','LAB','SEMINAR')),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(branch_id, room_number)
);

-- 13. users
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    phone_number VARCHAR(20),
    branch_id UUID REFERENCES branches(id) ON DELETE SET NULL,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
    last_login TIMESTAMPTZ,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 14. instructors
CREATE TABLE instructors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID UNIQUE NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    faculty_id UUID REFERENCES faculties(id) ON DELETE SET NULL,
    instructor_number VARCHAR(20) UNIQUE NOT NULL,
    academic_rank VARCHAR(30) DEFAULT 'LECTURER' CHECK (academic_rank IN ('LECTURER','ASSISTANT_PROFESSOR','ASSOCIATE_PROFESSOR','PROFESSOR')),
    specialization VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 15. students (StudentStatus split per Conflict 4)
CREATE TABLE students (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID UNIQUE NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    major_id UUID REFERENCES majors(id) ON DELETE SET NULL,
    student_number VARCHAR(20) UNIQUE NOT NULL,
    gpa DECIMAL(4,2) DEFAULT 0.00 CHECK (gpa BETWEEN 0 AND 4),
    completed_credits INT DEFAULT 0 CHECK (completed_credits >= 0),
    enrollment_date DATE DEFAULT CURRENT_DATE,
    status VARCHAR(20) DEFAULT 'ST_ACTIVE' CHECK (status IN ('ST_ACTIVE','ST_INACTIVE','ST_GRADUATED','ST_SUSPENDED')),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 16. course_sections
CREATE TABLE course_sections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE RESTRICT,
    semester_id UUID NOT NULL REFERENCES semesters(id) ON DELETE RESTRICT,
    classroom_id UUID REFERENCES classrooms(id) ON DELETE SET NULL,
    instructor_id UUID REFERENCES instructors(id) ON DELETE SET NULL,
    section_number VARCHAR(10) NOT NULL,
    max_capacity INT NOT NULL DEFAULT 30 CHECK (max_capacity > 0),
    current_enrollment INT DEFAULT 0 CHECK (current_enrollment >= 0 AND current_enrollment <= max_capacity),
    schedule_days VARCHAR(20),
    start_time TIME,
    end_time TIME,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(course_id, semester_id, section_number)
);

-- 17. study_plans
CREATE TABLE study_plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    major_id UUID NOT NULL REFERENCES majors(id) ON DELETE CASCADE,
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    semester_number INT NOT NULL CHECK (semester_number BETWEEN 1 AND 12),
    is_mandatory BOOLEAN DEFAULT TRUE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(major_id, course_id)
);

-- 18. course_enrollments (Core - EnrollRequestDto)
CREATE TABLE course_enrollments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    section_id UUID NOT NULL REFERENCES course_sections(id) ON DELETE RESTRICT,
    semester_id UUID NOT NULL REFERENCES semesters(id) ON DELETE RESTRICT,
    enrollment_date TIMESTAMPTZ DEFAULT NOW(),
    status VARCHAR(20) DEFAULT 'ENROLLED' CHECK (status IN ('ENROLLED','DROPPED','WITHDRAWN','COMPLETED','FAILED')),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(student_id, section_id, semester_id)
);

-- 19. attendance_records (FIXED - now has is_active)
CREATE TABLE attendance_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    enrollment_id UUID NOT NULL REFERENCES course_enrollments(id) ON DELETE CASCADE,
    attendance_date DATE NOT NULL,
    is_present BOOLEAN NOT NULL,
    notes VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(enrollment_id, attendance_date)
);

-- 20. grades
CREATE TABLE grades (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    enrollment_id UUID NOT NULL REFERENCES course_enrollments(id) ON DELETE CASCADE,
    midterm_score DECIMAL(5,2) CHECK (midterm_score BETWEEN 0 AND 100),
    final_score DECIMAL(5,2) CHECK (final_score BETWEEN 0 AND 100),
    total_score DECIMAL(5,2) CHECK (total_score BETWEEN 0 AND 100),
    letter_grade VARCHAR(5) CHECK (letter_grade IN ('A','B','C','D','F','I','W')),
    grade_points DECIMAL(3,2) CHECK (grade_points BETWEEN 0 AND 4),
    is_locked BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(enrollment_id)
);

-- 21. academic_records (AcademicStanding split)
CREATE TABLE academic_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    semester_id UUID NOT NULL REFERENCES semesters(id) ON DELETE RESTRICT,
    semester_gpa DECIMAL(4,2) CHECK (semester_gpa BETWEEN 0 AND 4),
    cumulative_gpa DECIMAL(4,2) CHECK (cumulative_gpa BETWEEN 0 AND 4),
    total_credits INT DEFAULT 0 CHECK (total_credits >= 0),
    total_points DECIMAL(6,2) DEFAULT 0,
    academic_status VARCHAR(20) DEFAULT 'GOOD_STANDING' CHECK (academic_status IN ('GOOD_STANDING','PROBATION','SUSPENDED','HONORS','DEANS_LIST')),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(student_id, semester_id)
);

-- 22. tuition_fees
CREATE TABLE tuition_fees (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    major_id UUID NOT NULL REFERENCES majors(id) ON DELETE CASCADE,
    academic_year VARCHAR(9) NOT NULL,
    credit_hour_price DECIMAL(10,2) NOT NULL CHECK (credit_hour_price > 0),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(major_id, academic_year)
);

-- 23. financial_records
CREATE TABLE financial_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    semester_id UUID REFERENCES semesters(id) ON DELETE SET NULL,
    total_due DECIMAL(10,2) DEFAULT 0 CHECK (total_due >= 0),
    total_paid DECIMAL(10,2) DEFAULT 0 CHECK (total_paid >= 0),
    balance DECIMAL(10,2) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','CLOSED','OVERDUE')),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(student_id, semester_id)
);

-- 24. payments
CREATE TABLE payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    financial_id UUID NOT NULL REFERENCES financial_records(id) ON DELETE CASCADE,
    amount DECIMAL(10,2) NOT NULL CHECK (amount > 0),
    payment_method VARCHAR(20) CHECK (payment_method IN ('CASH','CARD','BANK_TRANSFER','ONLINE')) NOT NULL,
    payment_date TIMESTAMPTZ DEFAULT NOW(),
    transaction_id VARCHAR(100) UNIQUE,
    notes VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 25. scholarships
CREATE TABLE scholarships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    scholarship_name VARCHAR(100) NOT NULL,
    scholarship_code VARCHAR(20) UNIQUE NOT NULL,
    discount_percentage DECIMAL(5,2) CHECK (discount_percentage BETWEEN 0 AND 100) NOT NULL,
    max_amount DECIMAL(10,2) CHECK (max_amount > 0),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 26. financial_refunds
CREATE TABLE financial_refunds (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    financial_id UUID NOT NULL REFERENCES financial_records(id) ON DELETE CASCADE,
    amount DECIMAL(10,2) NOT NULL CHECK (amount > 0),
    reason VARCHAR(255) NOT NULL,
    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING','APPROVED','REJECTED')),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 27. guardians
CREATE TABLE guardians (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    full_name VARCHAR(100) NOT NULL,
    phone VARCHAR(20),
    email VARCHAR(100),
    relationship VARCHAR(50) NOT NULL,
    is_primary BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 28. audit_logs (SPECIAL - No BaseEntity, per Rule 11 and Conflict 1)
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    action VARCHAR(50) NOT NULL,
    entity VARCHAR(100),
    entity_id VARCHAR(50),
    table_name VARCHAR(50),
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(45),
    user_agent VARCHAR(255),
    timestamp TIMESTAMPTZ DEFAULT NOW(),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 29. system_notifications
CREATE TABLE system_notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(150) NOT NULL,
    message TEXT NOT NULL,
    notification_type VARCHAR(30) DEFAULT 'INFO' CHECK (notification_type IN ('INFO','WARNING','SUCCESS','ERROR')),
    is_read BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 30. graduation_requests
CREATE TABLE graduation_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    request_date TIMESTAMPTZ DEFAULT NOW(),
    expected_graduation_date DATE,
    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING','APPROVED','REJECTED','CLEARANCE_PENDING')),
    clearance_status VARCHAR(20) DEFAULT 'PENDING' CHECK (clearance_status IN ('PENDING','APPROVED','REJECTED')),
    gpa_at_request DECIMAL(4,2) CHECK (gpa_at_request BETWEEN 0 AND 4),
    total_credits_at_request INT CHECK (total_credits_at_request >= 0),
    notes TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_role ON users(role_id);
CREATE INDEX idx_students_number ON students(student_number);
CREATE INDEX idx_students_user ON students(user_id);
CREATE INDEX idx_course_enrollments_student ON course_enrollments(student_id);
CREATE INDEX idx_course_enrollments_section ON course_enrollments(section_id);
CREATE INDEX idx_course_enrollments_semester ON course_enrollments(semester_id);
CREATE INDEX idx_grades_enrollment ON grades(enrollment_id);
CREATE INDEX idx_audit_logs_user ON audit_logs(user_id);
CREATE INDEX idx_audit_logs_timestamp ON audit_logs(timestamp);
CREATE INDEX idx_course_sections_course_sem ON course_sections(course_id, semester_id);
CREATE INDEX idx_admin_dept_branch ON administrative_departments(branch_id);
CREATE INDEX idx_payments_financial ON payments(financial_id);

-- Seed Roles
INSERT INTO roles (id, role_name, description) VALUES 
 (gen_random_uuid(), 'ADMIN', 'System Administrator'),
 (gen_random_uuid(), 'STUDENT', 'Student User'),
 (gen_random_uuid(), 'FACULTY', 'Faculty Member'),
 (gen_random_uuid(), 'FINANCE_OFFICER', 'Finance Officer')
ON CONFLICT (role_name) DO NOTHING;

-- Seed Permissions with Module (Conflict 5 resolved)
INSERT INTO permissions (permission_name, description, module) VALUES
 ('AUTH_LOGIN', 'Can login', 'AUTH'),
 ('USER_READ', 'Can read users', 'USER'),
 ('USER_WRITE', 'Can write users', 'USER'),
 ('STUDENT_READ', 'Can read student data', 'STUDENT'),
 ('STUDENT_WRITE', 'Can write student data', 'STUDENT'),
 ('ENROLLMENT_READ', 'Can read enrollments', 'ENROLLMENT'),
 ('ENROLLMENT_WRITE', 'Can enroll courses', 'ENROLLMENT'),
 ('GRADE_READ', 'Can read grades', 'GRADE'),
 ('GRADE_WRITE', 'Can write grades', 'GRADE'),
 ('FINANCE_READ', 'Can read finance', 'FINANCE'),
 ('FINANCE_WRITE', 'Can write finance', 'FINANCE')
ON CONFLICT (permission_name) DO NOTHING;
