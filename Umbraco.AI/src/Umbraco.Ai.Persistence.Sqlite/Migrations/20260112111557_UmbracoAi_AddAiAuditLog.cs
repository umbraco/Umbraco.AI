using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddAiAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAiAuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCategory = table.Column<int>(type: "INTEGER", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Capability = table.Column<int>(type: "INTEGER", nullable: false),
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
                    DetailLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentAuditLogId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiAuditLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditLog_EntityId_EntityType",
                table: "umbracoAiAuditLog",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditLog_ProfileId",
                table: "umbracoAiAuditLog",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditLog_StartTime",
                table: "umbracoAiAuditLog",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditLog_StartTime_Status",
                table: "umbracoAiAuditLog",
                columns: new[] { "StartTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditLog_Status",
                table: "umbracoAiAuditLog",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditLog_UserId",
                table: "umbracoAiAuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditLog_FeatureId",
                table: "umbracoAiAuditLog",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditLog_FeatureType_FeatureId",
                table: "umbracoAiAuditLog",
                columns: new[] { "FeatureType", "FeatureId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAuditLog_ParentAuditLogId",
                table: "umbracoAiAuditLog",
                column: "ParentAuditLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiAuditLog");
        }
    }
}
