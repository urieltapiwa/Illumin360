using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Recruitment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "signed_at",
                schema: "recruitment",
                table: "offers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signed_by_name",
                schema: "recruitment",
                table: "offers",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "signed_at",
                schema: "recruitment",
                table: "offers");

            migrationBuilder.DropColumn(
                name: "signed_by_name",
                schema: "recruitment",
                table: "offers");
        }
    }
}
