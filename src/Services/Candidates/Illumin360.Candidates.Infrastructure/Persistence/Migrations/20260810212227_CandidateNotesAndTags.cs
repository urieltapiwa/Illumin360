using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Candidates.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CandidateNotesAndTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "candidate_notes",
                schema: "candidates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "candidate_tags",
                schema: "candidates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_tags", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_candidate_notes_candidate_id",
                schema: "candidates",
                table: "candidate_notes",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_tags_candidate_id_label",
                schema: "candidates",
                table: "candidate_tags",
                columns: new[] { "candidate_id", "label" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidate_notes",
                schema: "candidates");

            migrationBuilder.DropTable(
                name: "candidate_tags",
                schema: "candidates");
        }
    }
}
