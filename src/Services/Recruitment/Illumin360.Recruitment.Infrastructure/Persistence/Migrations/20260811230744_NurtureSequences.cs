using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Recruitment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NurtureSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nurture_enrollments",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    next_step_order = table.Column<int>(type: "integer", nullable: false),
                    next_send_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    enrolled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nurture_enrollments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nurture_sequences",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nurture_sequences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nurture_steps",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    delay_days = table.Column<int>(type: "integer", nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nurture_steps", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_nurture_enrollments_sequence_id_email",
                schema: "recruitment",
                table: "nurture_enrollments",
                columns: new[] { "sequence_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nurture_enrollments_status_next_send_at",
                schema: "recruitment",
                table: "nurture_enrollments",
                columns: new[] { "status", "next_send_at" });

            migrationBuilder.CreateIndex(
                name: "IX_nurture_steps_sequence_id",
                schema: "recruitment",
                table: "nurture_steps",
                column: "sequence_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nurture_enrollments",
                schema: "recruitment");

            migrationBuilder.DropTable(
                name: "nurture_sequences",
                schema: "recruitment");

            migrationBuilder.DropTable(
                name: "nurture_steps",
                schema: "recruitment");
        }
    }
}
