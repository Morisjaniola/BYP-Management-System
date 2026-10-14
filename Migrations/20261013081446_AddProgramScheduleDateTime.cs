using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BYPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramScheduleDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduleEnd",
                table: "YouthPrograms",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduleStart",
                table: "YouthPrograms",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduleEnd",
                table: "YouthPrograms");

            migrationBuilder.DropColumn(
                name: "ScheduleStart",
                table: "YouthPrograms");
        }
    }
}
