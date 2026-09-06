-- ROLLBACK: hr_microservice_db -> ums_db (if needed)
-- WARNING: Only use if migration failed

-- Step 1: Export from hr_microservice_db
\c hr_microservice_db;

COPY (
    SELECT 
        external_user_id as id, -- Restore Shared PK
        employee_number,
        full_name,
        email,
        phone,
        department_id,
        branch_id,
        contract_type,
        status,
        hire_date,
        created_at
    FROM employees
) TO '/tmp/employees_rollback.csv' WITH CSV HEADER;

-- Step 2: Import back to ums_db (manual)
\c ums_db;

-- TRUNCATE employees; -- DANGEROUS - only if you want to restore
-- COPY employees(id, employee_number, full_name, email, phone, department_id, branch_id, contract_type, status, hire_date, created_at)
-- FROM '/tmp/employees_rollback.csv' WITH CSV HEADER;
