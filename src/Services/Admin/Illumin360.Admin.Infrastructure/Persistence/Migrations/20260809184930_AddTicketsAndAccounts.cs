using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Admin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketsAndAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    priority = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    requester = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assignee = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_status",
                schema: "admin",
                table: "accounts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_status",
                schema: "admin",
                table: "tickets",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounts",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "tickets",
                schema: "admin");
        }
    }
}
