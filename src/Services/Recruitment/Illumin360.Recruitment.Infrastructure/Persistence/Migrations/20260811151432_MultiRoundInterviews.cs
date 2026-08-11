using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Recruitment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MultiRoundInterviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "required_skills",
                schema: "recruitment",
                table: "interviews",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "round",
                schema: "recruitment",
                table: "interviews",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "interview_skill_ratings",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_skill_ratings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_interview_skill_ratings_interview_id",
                schema: "recruitment",
                table: "interview_skill_ratings",
                column: "interview_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interview_skill_ratings",
                schema: "recruitment");

            migrationBuilder.DropColumn(
                name: "required_skills",
                schema: "recruitment",
                table: "interviews");

            migrationBuilder.DropColumn(
                name: "round",
                schema: "recruitment",
                table: "interviews");
        }
    }
}
