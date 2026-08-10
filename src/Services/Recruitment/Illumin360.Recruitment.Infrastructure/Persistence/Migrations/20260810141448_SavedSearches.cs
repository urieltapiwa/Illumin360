using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Recruitment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SavedSearches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saved_searches",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    talent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    keyword = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    alerts_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_searches", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_saved_searches_talent_id",
                schema: "recruitment",
                table: "saved_searches",
                column: "talent_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saved_searches",
                schema: "recruitment");
        }
    }
}
