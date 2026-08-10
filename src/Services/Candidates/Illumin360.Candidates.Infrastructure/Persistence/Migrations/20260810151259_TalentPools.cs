using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Candidates.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TalentPools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "talent_pool_members",
                schema: "candidates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_talent_pool_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "talent_pools",
                schema: "candidates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_talent_pools", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_talent_pool_members_pool_id",
                schema: "candidates",
                table: "talent_pool_members",
                column: "pool_id");

            migrationBuilder.CreateIndex(
                name: "IX_talent_pool_members_pool_id_candidate_id",
                schema: "candidates",
                table: "talent_pool_members",
                columns: new[] { "pool_id", "candidate_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "talent_pool_members",
                schema: "candidates");

            migrationBuilder.DropTable(
                name: "talent_pools",
                schema: "candidates");
        }
    }
}
