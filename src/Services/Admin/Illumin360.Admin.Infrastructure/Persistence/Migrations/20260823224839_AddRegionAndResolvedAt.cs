using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Admin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionAndResolvedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "resolved_at",
                schema: "admin",
                table: "tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "admin",
                table: "accounts",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            // Backfill existing seed rows so the new region/resolved trends have data immediately.
            // No-ops on a fresh database (tables empty here — the seeder populates those with the same values).
            migrationBuilder.Sql(
                """
                UPDATE admin.accounts SET region='Windhoek'   WHERE id='ad300001-0000-4000-8000-000000000001';
                UPDATE admin.accounts SET region='Windhoek'   WHERE id='ad300001-0000-4000-8000-000000000002';
                UPDATE admin.accounts SET region='Walvis Bay' WHERE id='ad300001-0000-4000-8000-000000000003';
                UPDATE admin.accounts SET region='Oshakati'   WHERE id='ad300001-0000-4000-8000-000000000004';
                UPDATE admin.accounts SET region='Swakopmund' WHERE id='ad300001-0000-4000-8000-000000000005';
                UPDATE admin.accounts SET region='Windhoek'   WHERE id='ad300001-0000-4000-8000-000000000006';
                UPDATE admin.tickets  SET status='resolved', resolved_at = now() - interval '8 days', assignee = COALESCE(assignee,'support.agent') WHERE id='ad200001-0000-4000-8000-000000000004';
                UPDATE admin.tickets  SET status='resolved', resolved_at = now() - interval '5 days', assignee = COALESCE(assignee,'support.agent') WHERE id='ad200001-0000-4000-8000-000000000005';
                UPDATE admin.tickets  SET resolved_at = now() - interval '3 days' WHERE status='resolved' AND resolved_at IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "resolved_at",
                schema: "admin",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "region",
                schema: "admin",
                table: "accounts");
        }
    }
}
