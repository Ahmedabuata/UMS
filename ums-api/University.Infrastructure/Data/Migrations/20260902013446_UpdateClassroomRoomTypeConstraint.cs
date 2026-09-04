using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClassroomRoomTypeConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE classrooms DROP CONSTRAINT IF EXISTS classrooms_room_type_check; " +
                "ALTER TABLE classrooms ADD CONSTRAINT classrooms_room_type_check CHECK (" +
                "room_type IN ('LECTURE','LAB','SEMINAR','AUDITORIUM','COMPUTERLAB'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE classrooms DROP CONSTRAINT IF EXISTS classrooms_room_type_check; " +
                "ALTER TABLE classrooms ADD CONSTRAINT classrooms_room_type_check CHECK (" +
                "room_type IN ('LECTURE','LAB','SEMINAR'));");
        }
    }
}
