using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Professionals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MatchStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "professionals",
                table: "professional_matches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "new");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                schema: "professionals",
                table: "professional_matches");
        }
    }
}
