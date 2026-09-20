using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Employers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "subject",
                schema: "employers",
                table: "employers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_employers_subject",
                schema: "employers",
                table: "employers",
                column: "subject",
                unique: true,
                filter: "subject IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employers_subject",
                schema: "employers",
                table: "employers");

            migrationBuilder.DropColumn(
                name: "subject",
                schema: "employers",
                table: "employers");
        }
    }
}
