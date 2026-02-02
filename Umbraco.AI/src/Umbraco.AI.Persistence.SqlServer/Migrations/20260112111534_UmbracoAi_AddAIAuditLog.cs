using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddAiAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAIAuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorCategory = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Capability = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileAlias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FeatureType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    TotalTokens = table.Column<int>(type: "int", nullable: true),
                    PromptSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetailLevel = table.Column<int>(type: "int", nullable: false),
                    ParentAuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIAuditLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAuditLog_EntityId_EntityType",
                table: "umbracoAIAuditLog",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAuditLog_ProfileId",
                table: "umbracoAIAuditLog",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAuditLog_StartTime",
                table: "umbracoAIAuditLog",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAuditLog_StartTime_Status",
                table: "umbracoAIAuditLog",
                columns: new[] { "StartTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAuditLog_Status",
                table: "umbracoAIAuditLog",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAuditLog_UserId",
                table: "umbracoAIAuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAuditLog_FeatureId",
                table: "umbracoAIAuditLog",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAuditLog_FeatureType_FeatureId",
                table: "umbracoAIAuditLog",
                columns: new[] { "FeatureType", "FeatureId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAuditLog_ParentAuditLogId",
                table: "umbracoAIAuditLog",
                column: "ParentAuditLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAIAuditLog");
        }
    }
}
