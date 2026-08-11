using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Recruitment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TalentSideFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "city_signal",
                schema: "recruitment",
                table: "match_outcomes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "role_signal",
                schema: "recruitment",
                table: "match_outcomes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "skill_signal",
                schema: "recruitment",
                table: "match_outcomes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "application_features",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_signal = table.Column<int>(type: "integer", nullable: false),
                    role_signal = table.Column<int>(type: "integer", nullable: false),
                    skill_signal = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_features", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_application_features_application_id",
                schema: "recruitment",
                table: "application_features",
                column: "application_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_features",
                schema: "recruitment");

            migrationBuilder.DropColumn(
                name: "city_signal",
                schema: "recruitment",
                table: "match_outcomes");

            migrationBuilder.DropColumn(
                name: "role_signal",
                schema: "recruitment",
                table: "match_outcomes");

            migrationBuilder.DropColumn(
                name: "skill_signal",
                schema: "recruitment",
                table: "match_outcomes");
        }
    }
}
