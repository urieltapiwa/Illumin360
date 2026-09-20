using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Students.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "subject",
                schema: "students",
                table: "students",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_subject",
                schema: "students",
                table: "students",
                column: "subject",
                unique: true,
                filter: "subject IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_students_subject",
                schema: "students",
                table: "students");

            migrationBuilder.DropColumn(
                name: "subject",
                schema: "students",
                table: "students");
        }
    }
}
