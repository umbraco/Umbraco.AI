using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddAiAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAiAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TraceId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SpanId = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCategory = table.Column<int>(type: "INTEGER", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    OperationType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileAlias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FeatureType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FeatureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    InputTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    PromptSnapshot = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseSnapshot = table.Column<string>(type: "TEXT", nullable: true),
                    DetailLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAiAuditActivity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuditId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActivityId = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ParentActivityId = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    ActivityName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ActivityType = table.Column<int>(type: "INTEGER", nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    InputData = table.Column<string>(type: "TEXT", nullable: true),
                    OutputData = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorData = table.Column<string>(type: "TEXT", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: true),
                    TokensUsed = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiAuditActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiAuditActivity_umbracoAiAudit_AuditId",
                        column: x => x.AuditId,
                        principalTable: "umbracoAiAudit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditActivity_ActivityId",
                table: "umbracoAiAuditActivity",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditActivity_AuditId",
                table: "umbracoAiAuditActivity",
                column: "AuditId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditActivity_AuditId_SequenceNumber",
                table: "umbracoAiAuditActivity",
                columns: new[] { "AuditId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAudit_EntityId_EntityType",
                table: "umbracoAiAudit",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAudit_ProfileId",
                table: "umbracoAiAudit",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAudit_StartTime",
                table: "umbracoAiAudit",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAudit_StartTime_Status",
                table: "umbracoAiAudit",
                columns: new[] { "StartTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAudit_Status",
                table: "umbracoAiAudit",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAudit_TraceId",
                table: "umbracoAiAudit",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAudit_UserId",
                table: "umbracoAiAudit",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAudit_FeatureId",
                table: "umbracoAiAudit",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAudit_FeatureType_FeatureId",
                table: "umbracoAiAudit",
                columns: new[] { "FeatureType", "FeatureId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiAuditActivity");

            migrationBuilder.DropTable(
                name: "umbracoAiAudit");
        }
    }
}
