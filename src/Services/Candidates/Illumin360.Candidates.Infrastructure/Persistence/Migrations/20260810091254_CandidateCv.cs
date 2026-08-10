using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illumin360.Candidates.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CandidateCv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cv_content_type",
                schema: "candidates",
                table: "candidates",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cv_file_name",
                schema: "candidates",
                table: "candidates",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cv_object_key",
                schema: "candidates",
                table: "candidates",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "cv_size",
                schema: "candidates",
                table: "candidates",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cv_uploaded_at",
                schema: "candidates",
                table: "candidates",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cv_content_type",
                schema: "candidates",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "cv_file_name",
                schema: "candidates",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "cv_object_key",
                schema: "candidates",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "cv_size",
                schema: "candidates",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "cv_uploaded_at",
                schema: "candidates",
                table: "candidates");
        }
    }
}
