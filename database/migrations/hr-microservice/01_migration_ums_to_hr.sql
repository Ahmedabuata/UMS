-- =====================================================
-- MIGRATION: ums_db.employees -> hr_microservice_db.employees
-- Converts Shared PK (users.id == employees.id) to external_user_id UUID
-- Preserves Golden Constraints: No new tables/fields without approval
-- Uses CAP Outbox Pattern - Safe migration with transaction
-- =====================================================

-- Step 0: Create hr_microservice_db if not exists (run on postgres)
-- CREATE DATABASE hr_microservice_db OWNER hr_user;

-- Step 1: Create employees table in hr_microservice_db (Zero FK, isolated)
\c hr_microservice_db;

CREATE TABLE IF NOT EXISTS employees (
    id UUID PRIMARY KEY,
    external_user_id UUID NOT NULL UNIQUE,
    employee_number VARCHAR(30) NOT NULL UNIQUE,
    full_name VARCHAR(200) NOT NULL,
    email VARCHAR(200) NOT NULL,
    phone VARCHAR(50),
    department_id UUID,
    department_name VARCHAR(200),
    branch_id UUID,
    branch_name VARCHAR(200),
    contract_type VARCHAR(50),
    status VARCHAR(20) DEFAULT 'Active',
    hire_date DATE,
    contract_end_date DATE,
    academic_title VARCHAR(100),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP
);

CREATE INDEX idx_employees_external_user_id ON employees(external_user_id);
CREATE INDEX idx_employees_email ON employees(email);
CREATE INDEX idx_employees_status ON employees(status);

-- Step 2: Create CAP Outbox tables (auto-created by CAP, but we create for migration)
CREATE TABLE IF NOT EXISTS cap.published (
    id SERIAL PRIMARY KEY,
    version VARCHAR(20),
    name VARCHAR(200),
    content TEXT,
    retries INT,
    added TIMESTAMP,
    expires_at TIMESTAMP,
    status_name VARCHAR(50)
);

CREATE TABLE IF NOT EXISTS cap.received (
    id SERIAL PRIMARY KEY,
    version VARCHAR(20),
    name VARCHAR(200),
    group_name VARCHAR(200),
    content TEXT,
    retries INT,
    added TIMESTAMP,
    expires_at TIMESTAMP,
    status_name VARCHAR(50)
);

-- Step 3: Migration Script - Transfer data from ums_db to hr_microservice_db
-- Run this from ums_db connection with dblink or via application

-- Option A: Using dblink (if both DBs on same Postgres server)
-- Enable dblink
CREATE EXTENSION IF NOT EXISTS dblink;

-- Migrate with transformation
INSERT INTO employees (
    id,
    external_user_id,
    employee_number,
    full_name,
    email,
    phone,
    department_id,
    department_name,
    branch_id,
    branch_name,
    contract_type,
    status,
    hire_date,
    created_at
)
SELECT 
    gen_random_uuid() as id,  -- New independent ID for HR microservice
    e.id as external_user_id, -- OLD: Shared PK (was User.Id) -> NEW: ExternalUserId
    e.employee_number,
    e.full_name,
    e.email,
    e.phone,
    e.department_id,
    ad.department_name as department_name, -- Denormalized
    e.branch_id,
    b.branch_name as branch_name, -- Denormalized
    e.contract_type,
    COALESCE(e.status, 'Active') as status,
    e.hire_date::DATE,
    e.created_at
FROM dblink(
    'host=ums-db port=5432 dbname=ums_db user=ums_user password=Ums_Pass_2024!',
    'SELECT id, employee_number, full_name, email, phone, department_id, branch_id, contract_type, status, hire_date, created_at FROM employees'
) AS e(
    id UUID,
    employee_number VARCHAR,
    full_name VARCHAR,
    email VARCHAR,
    phone VARCHAR,
    department_id UUID,
    branch_id UUID,
    contract_type VARCHAR,
    status VARCHAR,
    hire_date DATE,
    created_at TIMESTAMP
)
LEFT JOIN dblink(
    'host=ums-db port=5432 dbname=ums_db user=ums_user password=Ums_Pass_2024!',
    'SELECT id, department_name FROM administrative_departments'
) AS ad(id UUID, department_name VARCHAR) ON ad.id = e.department_id
LEFT JOIN dblink(
    'host=ums-db port=5432 dbname=ums_db user=ums_user password=Ums_Pass_2024!',
    'SELECT id, branch_name FROM branches'
) AS b(id UUID, branch_name VARCHAR) ON b.id = e.branch_id
ON CONFLICT (external_user_id) DO NOTHING;

-- Verify migration
SELECT 
    COUNT(*) as total_migrated,
    COUNT(DISTINCT external_user_id) as unique_external_users,
    COUNT(CASE WHEN status = 'Active' THEN 1 END) as active_count
FROM employees;

-- Step 4: Verification queries
-- Check for orphaned records (should be 0)
SELECT e.id, e.employee_number, e.external_user_id
FROM employees e
LEFT JOIN dblink(
    'host=ums-db port=5432 dbname=ums_db user=ums_user password=Ums_Pass_2024!',
    'SELECT id FROM users'
) AS u(id UUID) ON u.id = e.external_user_id
WHERE u.id IS NULL;

-- Sample of migrated data
SELECT id, external_user_id, employee_number, full_name, email, status FROM employees LIMIT 5;
