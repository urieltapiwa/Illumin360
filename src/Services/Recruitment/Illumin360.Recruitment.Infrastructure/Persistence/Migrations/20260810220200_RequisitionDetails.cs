using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Recruitment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequisitionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "requisition_details",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    salary_min = table.Column<int>(type: "integer", nullable: true),
                    salary_max = table.Column<int>(type: "integer", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    employment_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    remote = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requisition_details", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "requisition_tags",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requisition_tags", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_requisition_details_request_id",
                schema: "recruitment",
                table: "requisition_details",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_requisition_tags_request_id_label",
                schema: "recruitment",
                table: "requisition_tags",
                columns: new[] { "request_id", "label" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "requisition_details",
                schema: "recruitment");

            migrationBuilder.DropTable(
                name: "requisition_tags",
                schema: "recruitment");
        }
    }
}
