using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BYPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAfternoonAttendanceWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "AfternoonInEnd",
                table: "YouthPrograms",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeOnly(14, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "AfternoonInStart",
                table: "YouthPrograms",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeOnly(13, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "AfternoonOutEnd",
                table: "YouthPrograms",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeOnly(17, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "AfternoonOutStart",
                table: "YouthPrograms",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeOnly(16, 0, 0));

            migrationBuilder.AddColumn<DateTime>(
                name: "AfternoonInAt",
                table: "AttendanceRecords",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AfternoonOutAt",
                table: "AttendanceRecords",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfternoonInEnd",
                table: "YouthPrograms");

            migrationBuilder.DropColumn(
                name: "AfternoonInStart",
                table: "YouthPrograms");

            migrationBuilder.DropColumn(
                name: "AfternoonOutEnd",
                table: "YouthPrograms");

            migrationBuilder.DropColumn(
                name: "AfternoonOutStart",
                table: "YouthPrograms");

            migrationBuilder.DropColumn(
                name: "AfternoonInAt",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "AfternoonOutAt",
                table: "AttendanceRecords");
        }
    }
}
