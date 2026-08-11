using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Recruitment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReferralsAndInternalRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "internal",
                schema: "recruitment",
                table: "requisition_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "referrals",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    referrer_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    referrer_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    candidate_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    candidate_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referrals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_referrals_request_id",
                schema: "recruitment",
                table: "referrals",
                column: "request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "referrals",
                schema: "recruitment");

            migrationBuilder.DropColumn(
                name: "internal",
                schema: "recruitment",
                table: "requisition_details");
        }
    }
}
