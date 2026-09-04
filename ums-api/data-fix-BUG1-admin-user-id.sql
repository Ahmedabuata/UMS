-- ============================================================================
-- BUG #1 Data Fix (PostgreSQL)
-- ----------------------------------------------------------------------------
-- Root cause: when the manual admin login row was created, a NEW random UUID
-- was generated for users.id instead of reusing the employee's UUID. Because
-- the schema uses a shared primary key (users.id == employees.id; the user row
-- is the FK side referencing employees.id), the mismatched id broke profile
-- resolution and search (e.g. the admin showed a dashed/garbled value in the
-- Users tab, and searches by EMP-ADM-001 / Ahmed / ADM-HR-202609-00001 failed).
--
-- This script repairs that row IN PLACE (no schema change): it points users.id
-- back to employees.id for the affected admin employee.
--
-- IMPORTANT: run inside a transaction so the fix is atomic, and back up first.
-- ============================================================================

BEGIN;

-- 0) Sanity check: confirm the mismatch exists (a user row whose id != employee id).
SELECT e.id        AS employee_id,
       e.employee_number,
       e.email,
       u.id        AS current_user_id,
       u.username
FROM   employees e
JOIN   users u ON u.username = e.email
WHERE  e.employee_number = 'EMP-ADM-001'
  AND  u.id <> e.id;

-- 1) Repair child join rows that reference the OLD (wrong) user id.
UPDATE user_roles ur
SET    user_id = e.id
FROM   employees e
WHERE  ur.user_id IN (
           SELECT u.id FROM users u
           JOIN employees ee ON u.username = ee.email
           WHERE ee.employee_number = 'EMP-ADM-001'
       )
  AND  e.employee_number = 'EMP-ADM-001';

UPDATE user_groups ug
SET    user_id = e.id
FROM   employees e
WHERE  ug.user_id IN (
           SELECT u.id FROM users u
           JOIN employees ee ON u.username = ee.email
           WHERE ee.employee_number = 'EMP-ADM-001'
       )
  AND  e.employee_number = 'EMP-ADM-001';

-- 2) Point the user PK back to the employee's UUID.
UPDATE users u
SET    id = e.id
FROM   employees e
WHERE  e.employee_number = 'EMP-ADM-001'
  AND  u.id <> e.id
  AND  u.username = e.email;

COMMIT;

-- 3) Verify: the row should now show one user whose id equals the employee id.
SELECT e.id AS employee_id,
       e.employee_number,
       u.id AS user_id,
       u.username
FROM   employees e
LEFT   JOIN users u ON u.id = e.id
WHERE  e.employee_number = 'EMP-ADM-001';
