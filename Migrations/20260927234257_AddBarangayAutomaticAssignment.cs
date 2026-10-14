using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BYPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBarangayAutomaticAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "YouthPrograms",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "YouthParticipants",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "YouthPrograms");

            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "YouthParticipants");
        }
    }
}
