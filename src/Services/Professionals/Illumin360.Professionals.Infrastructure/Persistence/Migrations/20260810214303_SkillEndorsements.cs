using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Professionals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SkillEndorsements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "endorsements",
                schema: "professionals",
                table: "professional_skills",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "skill_endorsements",
                schema: "professionals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endorser = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_endorsements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_skill_endorsements_skill_id",
                schema: "professionals",
                table: "skill_endorsements",
                column: "skill_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "skill_endorsements",
                schema: "professionals");

            migrationBuilder.DropColumn(
                name: "endorsements",
                schema: "professionals",
                table: "professional_skills");
        }
    }
}
