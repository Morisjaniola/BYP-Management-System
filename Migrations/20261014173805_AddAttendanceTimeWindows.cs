using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BYPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceTimeWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "TimeInEnd",
                table: "YouthPrograms",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeOnly(8, 30, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "TimeInStart",
                table: "YouthPrograms",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeOnly(7, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "TimeOutEnd",
                table: "YouthPrograms",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeOnly(11, 30, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "TimeOutStart",
                table: "YouthPrograms",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeOnly(10, 30, 0));

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeInAt",
                table: "AttendanceRecords",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeOutAt",
                table: "AttendanceRecords",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "AttendancePhotoSubmissions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "TimeIn")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AttendanceAnnouncementLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Kind = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceAnnouncementLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // The replacement composite index must exist BEFORE the
            // single-column index is dropped, because the foreign key
            // on ProgramId requires a usable index at drop time.

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRegistrations_ProgramId_YouthParticipantId_UserId",
                table: "ProgramRegistrations",
                columns: new[] { "ProgramId", "YouthParticipantId", "UserId" });

            migrationBuilder.DropIndex(
                name: "IX_ProgramRegistrations_ProgramId",
                table: "ProgramRegistrations");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAnnouncementLogs_ProgramId_AttendanceDate_Kind",
                table: "AttendanceAnnouncementLogs",
                columns: new[] { "ProgramId", "AttendanceDate", "Kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceAnnouncementLogs");

            migrationBuilder.DropIndex(
                name: "IX_ProgramRegistrations_ProgramId_YouthParticipantId_UserId",
                table: "ProgramRegistrations");

            migrationBuilder.DropColumn(
                name: "TimeInEnd",
                table: "YouthPrograms");

            migrationBuilder.DropColumn(
                name: "TimeInStart",
                table: "YouthPrograms");

            migrationBuilder.DropColumn(
                name: "TimeOutEnd",
                table: "YouthPrograms");

            migrationBuilder.DropColumn(
                name: "TimeOutStart",
                table: "YouthPrograms");

            migrationBuilder.DropColumn(
                name: "TimeInAt",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "TimeOutAt",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "AttendancePhotoSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRegistrations_ProgramId",
                table: "ProgramRegistrations",
                column: "ProgramId");
        }
    }
}
