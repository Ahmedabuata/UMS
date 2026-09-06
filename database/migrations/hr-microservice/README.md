# HR Migration: ums_db -> hr_microservice_db

## What it does:
- Transforms Shared PK (users.id == employees.id) to external_user_id UUID
- Old: Employee.Id = User.Id (same UUID)
- New: Employee.Id = New Guid, Employee.ExternalUserId = Old User.Id

## Files:
1. 01_migration_ums_to_hr.sql - Direct SQL migration using dblink (fast, for same Postgres server)
2. 02_migration_tool.cs - C# tool with CAP Outbox (safe, for production, prevents message loss)
3. 03_ef_migration.cs - EF Core migration for HR DbContext (creates hr_microservice_db.employees)
4. 04_rollback.sql - Rollback if needed

## How to run:

### Option A: SQL (Quick - Same Server)
```bash
psql -U postgres -f 01_migration_ums_to_hr.sql
```

### Option B: C# Tool (Safe - With CAP Outbox)
```bash
dotnet run --project HR.MigrationTool
# Uses CAP to publish hr.migration.employee_migrated events
# Even if RabbitMQ down, messages stored in Outbox and retried
```

### Option C: Docker
```bash
docker exec -it ums-db psql -U ums_user -d ums_db -f /migration/01_migration_ums_to_hr.sql
```

## Golden Constraints Preserved:
- No new tables in ums_db
- No new fields in ums_db.employees
- hr_microservice_db is completely separate
- ExternalUserId is UUID only, NO Foreign Key
