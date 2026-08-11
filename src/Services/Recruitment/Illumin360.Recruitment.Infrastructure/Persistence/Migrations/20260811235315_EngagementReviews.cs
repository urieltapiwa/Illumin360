using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Recruitment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EngagementReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "engagement_reviews",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    talent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagement_reviews", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_engagement_reviews_application_id_reviewer",
                schema: "recruitment",
                table: "engagement_reviews",
                columns: new[] { "application_id", "reviewer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_engagement_reviews_talent_id_visible",
                schema: "recruitment",
                table: "engagement_reviews",
                columns: new[] { "talent_id", "visible" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "engagement_reviews",
                schema: "recruitment");
        }
    }
}
