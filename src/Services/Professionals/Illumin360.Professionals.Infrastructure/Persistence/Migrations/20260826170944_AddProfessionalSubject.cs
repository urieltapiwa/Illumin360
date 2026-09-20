using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Professionals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionalSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "subject",
                schema: "professionals",
                table: "professionals",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_professionals_subject",
                schema: "professionals",
                table: "professionals",
                column: "subject",
                unique: true,
                filter: "subject IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_professionals_subject",
                schema: "professionals",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "subject",
                schema: "professionals",
                table: "professionals");
        }
    }
}
