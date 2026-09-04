using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdToBuilding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "buildings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_buildings_branch_id",
                table: "buildings",
                column: "branch_id");

            migrationBuilder.AddForeignKey(
                name: "FK_buildings_branches_branch_id",
                table: "buildings",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_buildings_branches_branch_id",
                table: "buildings");

            migrationBuilder.DropIndex(
                name: "IX_buildings_branch_id",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "buildings");
        }
    }
}
