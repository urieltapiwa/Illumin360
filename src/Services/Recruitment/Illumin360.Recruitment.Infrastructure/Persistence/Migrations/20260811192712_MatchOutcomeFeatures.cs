using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Recruitment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MatchOutcomeFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "avg_interview_rating",
                schema: "recruitment",
                table: "match_outcomes",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "days_to_decision",
                schema: "recruitment",
                table: "match_outcomes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "had_offer",
                schema: "recruitment",
                table: "match_outcomes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "interview_count",
                schema: "recruitment",
                table: "match_outcomes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "remote",
                schema: "recruitment",
                table: "match_outcomes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "source",
                schema: "recruitment",
                table: "match_outcomes",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avg_interview_rating",
                schema: "recruitment",
                table: "match_outcomes");

            migrationBuilder.DropColumn(
                name: "days_to_decision",
                schema: "recruitment",
                table: "match_outcomes");

            migrationBuilder.DropColumn(
                name: "had_offer",
                schema: "recruitment",
                table: "match_outcomes");

            migrationBuilder.DropColumn(
                name: "interview_count",
                schema: "recruitment",
                table: "match_outcomes");

            migrationBuilder.DropColumn(
                name: "remote",
                schema: "recruitment",
                table: "match_outcomes");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "recruitment",
                table: "match_outcomes");
        }
    }
}
