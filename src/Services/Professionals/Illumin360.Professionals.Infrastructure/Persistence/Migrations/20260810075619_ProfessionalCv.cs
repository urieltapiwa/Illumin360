using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Professionals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProfessionalCv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cv_content_type",
                schema: "professionals",
                table: "professionals",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cv_file_name",
                schema: "professionals",
                table: "professionals",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cv_object_key",
                schema: "professionals",
                table: "professionals",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "cv_size",
                schema: "professionals",
                table: "professionals",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cv_uploaded_at",
                schema: "professionals",
                table: "professionals",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cv_content_type",
                schema: "professionals",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "cv_file_name",
                schema: "professionals",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "cv_object_key",
                schema: "professionals",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "cv_size",
                schema: "professionals",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "cv_uploaded_at",
                schema: "professionals",
                table: "professionals");
        }
    }
}
