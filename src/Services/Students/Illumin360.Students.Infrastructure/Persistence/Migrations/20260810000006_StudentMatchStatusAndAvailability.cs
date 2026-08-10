using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Students.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StudentMatchStatusAndAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "availability",
                schema: "students",
                table: "students",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "Open to internships");

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "students",
                table: "student_matches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "new");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "availability",
                schema: "students",
                table: "students");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "students",
                table: "student_matches");
        }
    }
}
