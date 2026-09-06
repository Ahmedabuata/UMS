// File: HR.Infrastructure/Data/Migrations/20260904000000_InitialHrMicroservice.cs
using Microsoft.EntityFrameworkCore.Migrations;

namespace HR.Infrastructure.Data.Migrations;

public partial class InitialHrMicroservice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create employees table - isolated, no FK
        migrationBuilder.CreateTable(
            name: "employees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                external_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                employee_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                department_id = table.Column<Guid>(type: "uuid", nullable: true),
                department_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                branch_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                contract_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active"),
                hire_date = table.Column<DateOnly>(type: "date", nullable: true),
                contract_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                academic_title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_employees", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_employees_external_user_id",
            table: "employees",
            column: "external_user_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_employees_employee_number",
            table: "employees",
            column: "employee_number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_employees_email",
            table: "employees",
            column: "email");

        // CAP tables for Outbox Pattern (auto-created by CAP, but we ensure)
        migrationBuilder.Sql(@"
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
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "employees");
        migrationBuilder.Sql("DROP TABLE IF EXISTS cap.published; DROP TABLE IF EXISTS cap.received;");
    }
}
