using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeStudentInstructorUserLinkOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_instructors_users_user_id",
                table: "instructors");

            migrationBuilder.DropForeignKey(
                name: "FK_students_users_user_id",
                table: "students");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "students",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "instructors",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_instructors_users_user_id",
                table: "instructors",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_students_users_user_id",
                table: "students",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Seed admission records so Create User validation ("must exist before an account")
            // has real records to match against. user_id intentionally NULL until a user is linked.
            // NOTE: instructors are NOT seeded here because under Method-2 (shared primary key)
            // instructors must be employees (instructors.id == employees.id); standalone instructor
            // rows would break the ReverseFK_SharedPK_SameUUID FK (instructors.id -> employees.id).
            migrationBuilder.InsertData(
                table: "students",
                columns: new[] { "id", "user_id", "major_id", "student_number", "gpa", "completed_credits", "enrollment_date", "status", "is_active", "created_at", "updated_at" },
                values: new object[,]
                {
                    { Guid.Parse("a1b2c3d4-0000-0000-0000-000000000101"), null, null, "20210001", 3.45m, 96, new DateOnly(2021, 9, 1), "ST_ACTIVE", true, new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { Guid.Parse("a1b2c3d4-0000-0000-0000-000000000102"), null, null, "20220007", 0m, 0, new DateOnly(2022, 9, 1), "ST_ACTIVE", true, new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_instructors_users_user_id",
                table: "instructors");

            migrationBuilder.DropForeignKey(
                name: "FK_students_users_user_id",
                table: "students");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "students",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "instructors",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_instructors_users_user_id",
                table: "instructors",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_students_users_user_id",
                table: "students",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DeleteData(
                table: "students",
                keyColumn: "id",
                keyValue: Guid.Parse("a1b2c3d4-0000-0000-0000-000000000101"));
            migrationBuilder.DeleteData(
                table: "students",
                keyColumn: "id",
                keyValue: Guid.Parse("a1b2c3d4-0000-0000-0000-000000000102"));
        }
    }
}
