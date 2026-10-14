using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BYPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddYouthParticipantToRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ProgramRegistrations",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(450)",
                oldMaxLength: 450)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "YouthParticipantId",
                table: "ProgramRegistrations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRegistrations_YouthParticipantId",
                table: "ProgramRegistrations",
                column: "YouthParticipantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramRegistrations_YouthParticipants_YouthParticipantId",
                table: "ProgramRegistrations",
                column: "YouthParticipantId",
                principalTable: "YouthParticipants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramRegistrations_YouthParticipants_YouthParticipantId",
                table: "ProgramRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_ProgramRegistrations_YouthParticipantId",
                table: "ProgramRegistrations");

            migrationBuilder.DropColumn(
                name: "YouthParticipantId",
                table: "ProgramRegistrations");

            migrationBuilder.UpdateData(
                table: "ProgramRegistrations",
                keyColumn: "UserId",
                keyValue: null,
                column: "UserId",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ProgramRegistrations",
                type: "varchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(450)",
                oldMaxLength: 450,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
