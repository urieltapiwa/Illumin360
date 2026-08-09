using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Illumin360.Professionals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "professionals");

            migrationBuilder.CreateTable(
                name: "InboxState",
                schema: "professionals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Received = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiveCount = table.Column<int>(type: "integer", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Consumed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxState", x => x.Id);
                    table.UniqueConstraint("AK_InboxState_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "OutboxState",
                schema: "professionals",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxState", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "professional_activity",
                schema: "professionals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    professional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    icon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    when_label = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professional_activity", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "professional_matches",
                schema: "professionals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    professional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    company = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    industry = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    match_score = table.Column<int>(type: "integer", nullable: false),
                    salary_lo = table.Column<int>(type: "integer", nullable: false),
                    salary_hi = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    posted_label = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professional_matches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "professional_pipeline",
                schema: "professionals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    professional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professional_pipeline", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "professional_skill_demand",
                schema: "professionals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    professional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professional_skill_demand", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "professional_skills",
                schema: "professionals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    professional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    trend = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professional_skills", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "professionals",
                schema: "professionals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    availability = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    headline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    profile_strength = table.Column<int>(type: "integer", nullable: false),
                    percentile = table.Column<int>(type: "integer", nullable: false),
                    member_since = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    profile_views = table.Column<int>(type: "integer", nullable: false),
                    views_delta = table.Column<int>(type: "integer", nullable: false),
                    match_opportunities = table.Column<int>(type: "integer", nullable: false),
                    match_delta = table.Column<int>(type: "integer", nullable: false),
                    active_applications = table.Column<int>(type: "integer", nullable: false),
                    response_rate = table.Column<int>(type: "integer", nullable: false),
                    avg_match = table.Column<int>(type: "integer", nullable: false),
                    interviews = table.Column<int>(type: "integer", nullable: false),
                    views_trend = table.Column<int[]>(type: "integer[]", nullable: false),
                    salary_role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    salary_p25 = table.Column<int>(type: "integer", nullable: false),
                    salary_median = table.Column<int>(type: "integer", nullable: false),
                    salary_p75 = table.Column<int>(type: "integer", nullable: false),
                    salary_you = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professionals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "professionals",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnqueueTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    InboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboxConsumerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ResponseAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FaultAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.SequenceNumber);
                    table.ForeignKey(
                        name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                        columns: x => new { x.InboxMessageId, x.InboxConsumerId },
                        principalSchema: "professionals",
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalSchema: "professionals",
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                schema: "professionals",
                table: "InboxState",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                schema: "professionals",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                schema: "professionals",
                table: "OutboxMessage",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
                schema: "professionals",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_OutboxId_SequenceNumber",
                schema: "professionals",
                table: "OutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                schema: "professionals",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_professional_activity_professional_id",
                schema: "professionals",
                table: "professional_activity",
                column: "professional_id");

            migrationBuilder.CreateIndex(
                name: "IX_professional_matches_professional_id",
                schema: "professionals",
                table: "professional_matches",
                column: "professional_id");

            migrationBuilder.CreateIndex(
                name: "IX_professional_pipeline_professional_id",
                schema: "professionals",
                table: "professional_pipeline",
                column: "professional_id");

            migrationBuilder.CreateIndex(
                name: "IX_professional_skill_demand_professional_id",
                schema: "professionals",
                table: "professional_skill_demand",
                column: "professional_id");

            migrationBuilder.CreateIndex(
                name: "IX_professional_skills_professional_id",
                schema: "professionals",
                table: "professional_skills",
                column: "professional_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessage",
                schema: "professionals");

            migrationBuilder.DropTable(
                name: "professional_activity",
                schema: "professionals");

            migrationBuilder.DropTable(
                name: "professional_matches",
                schema: "professionals");

            migrationBuilder.DropTable(
                name: "professional_pipeline",
                schema: "professionals");

            migrationBuilder.DropTable(
                name: "professional_skill_demand",
                schema: "professionals");

            migrationBuilder.DropTable(
                name: "professional_skills",
                schema: "professionals");

            migrationBuilder.DropTable(
                name: "professionals",
                schema: "professionals");

            migrationBuilder.DropTable(
                name: "InboxState",
                schema: "professionals");

            migrationBuilder.DropTable(
                name: "OutboxState",
                schema: "professionals");
        }
    }
}
