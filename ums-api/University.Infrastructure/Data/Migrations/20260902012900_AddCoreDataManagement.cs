using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreDataManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "registration_end",
                table: "semesters",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "registration_start",
                table: "semesters",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "semesters",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "branch_id",
                table: "classrooms",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "building_id",
                table: "classrooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "floor",
                table: "classrooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "buildings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    floors = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_classrooms_building_id",
                table: "classrooms",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_buildings_code",
                table: "buildings",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_classrooms_buildings_building_id",
                table: "classrooms",
                column: "building_id",
                principalTable: "buildings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_classrooms_buildings_building_id",
                table: "classrooms");

            migrationBuilder.DropTable(
                name: "buildings");

            migrationBuilder.DropIndex(
                name: "IX_classrooms_building_id",
                table: "classrooms");

            migrationBuilder.DropColumn(
                name: "registration_end",
                table: "semesters");

            migrationBuilder.DropColumn(
                name: "registration_start",
                table: "semesters");

            migrationBuilder.DropColumn(
                name: "status",
                table: "semesters");

            migrationBuilder.DropColumn(
                name: "building_id",
                table: "classrooms");

            migrationBuilder.DropColumn(
                name: "floor",
                table: "classrooms");

            migrationBuilder.AlterColumn<Guid>(
                name: "branch_id",
                table: "classrooms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
