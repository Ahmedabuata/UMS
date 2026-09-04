-- 02-create-employees.sql
-- UMS HR Module: employees master table + instructors.employee_id link.
-- Idempotent: safe to run against an existing ums_db (post-init).
-- Target: ums_db (Postgres), matches EF entity `Employee` (University.Core/Entities/Employee.cs).

-- ------------------------------------------------------------------
-- 1) employees table
-- ------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS employees (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_number VARCHAR(50) UNIQUE NOT NULL,
    full_name      VARCHAR(100) NOT NULL,
    email          VARCHAR(100) UNIQUE NOT NULL,
    phone          VARCHAR(20),
    department_id  UUID REFERENCES administrative_departments(id) ON DELETE SET NULL,
    branch_id      UUID REFERENCES branches(id) ON DELETE SET NULL,
    contract_type  VARCHAR(20) DEFAULT 'Full-time',
    status         VARCHAR(20) DEFAULT 'Active',
    hire_date      DATE,
    user_id        UUID REFERENCES users(id) ON DELETE SET NULL,
    is_active      BOOLEAN DEFAULT TRUE,
    created_at     TIMESTAMPTZ DEFAULT NOW(),
    updated_at     TIMESTAMPTZ DEFAULT NOW()
);

-- ------------------------------------------------------------------
-- 2) instructors.employee_id (المحاضر هو موظف)
-- ------------------------------------------------------------------
ALTER TABLE instructors ADD COLUMN IF NOT EXISTS employee_id UUID REFERENCES employees(id) ON DELETE SET NULL;

-- ------------------------------------------------------------------
-- 3) seed employees (deterministic IDs)
-- ------------------------------------------------------------------
INSERT INTO employees (id, employee_number, full_name, email, phone, department_id, branch_id, contract_type, status, hire_date, user_id, is_active)
VALUES
    ('e0000000-0000-0000-0000-000000000001', 'EMP-001', 'Dr. Khalid Mourad', 'khalid@uni.edu', '0555000003',
     'eac13faf-d29c-4370-9a9a-dd29c888c06b', '1f522f45-35d9-4b2f-ab35-76e50d845958', 'Full-time', 'Active', '2019-09-01',
     'b0000000-0000-0000-0000-000000000003', TRUE),
    ('e0000000-0000-0000-0000-000000000002', 'EMP-002', 'Huda Hassan', 'huda@uni.edu', '0555000002',
     '702b46f5-e553-4d28-9f63-f00a9c23eb84', '3e0a1d43-2b6c-4d8e-9a11-7c6f5e4d3b2a', 'Full-time', 'Active', '2020-03-15',
     'b0000000-0000-0000-0000-000000000002', TRUE),
    ('e0000000-0000-0000-0000-000000000003', 'EMP-003', 'Omar Saleh', 'omar@uni.edu', '0555000001',
     '77bc056d-0a0f-456c-889c-78b17449dfe5', '1f522f45-35d9-4b2f-ab35-76e50d845958', 'Contract', 'On Leave', '2021-06-01',
     NULL, TRUE)
ON CONFLICT (employee_number) DO NOTHING;

-- ------------------------------------------------------------------
-- 4) link instructors to their employee record (المحاضر هو موظف)
--    INS-001 -> EMP-001 (Dr. Khalid Mourad), INS-002 -> EMP-002 (Huda Hassan)
-- ------------------------------------------------------------------
UPDATE instructors SET employee_id = 'e0000000-0000-0000-0000-000000000001', updated_at = NOW() WHERE instructor_number = 'INS-001';
UPDATE instructors SET employee_id = 'e0000000-0000-0000-0000-000000000002', updated_at = NOW() WHERE instructor_number = 'INS-002';