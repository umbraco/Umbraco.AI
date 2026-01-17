using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddUsageAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAiUsageRecord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Capability = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileAlias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FeatureType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InputTokens = table.Column<int>(type: "bigint", nullable: false),
                    OutputTokens = table.Column<int>(type: "bigint", nullable: false),
                    TotalTokens = table.Column<int>(type: "bigint", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiUsageRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAiUsageStatisticsDaily",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Period = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Capability = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FeatureType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    InputTokens = table.Column<long>(type: "bigint", nullable: false),
                    OutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    TotalTokens = table.Column<long>(type: "bigint", nullable: false),
                    TotalDurationMs = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiUsageStatisticsDaily", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAiUsageStatisticsHourly",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Period = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Capability = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FeatureType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    InputTokens = table.Column<long>(type: "bigint", nullable: false),
                    OutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    TotalTokens = table.Column<long>(type: "bigint", nullable: false),
                    TotalDurationMs = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiUsageStatisticsHourly", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageRecord_Timestamp",
                table: "umbracoAiUsageRecord",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageRecord_Timestamp_Status",
                table: "umbracoAiUsageRecord",
                columns: new[] { "Timestamp", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageStatisticsDaily_Period",
                table: "umbracoAiUsageStatisticsDaily",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageStatisticsDaily_Period_ModelId",
                table: "umbracoAiUsageStatisticsDaily",
                columns: new[] { "Period", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageStatisticsDaily_Period_ProfileId",
                table: "umbracoAiUsageStatisticsDaily",
                columns: new[] { "Period", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageStatisticsDaily_Period_ProviderId",
                table: "umbracoAiUsageStatisticsDaily",
                columns: new[] { "Period", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageStatisticsDaily_Period_ProviderId_ModelId_ProfileId_Capability_UserId_EntityType_FeatureType",
                table: "umbracoAiUsageStatisticsDaily",
                columns: new[] { "Period", "ProviderId", "ModelId", "ProfileId", "Capability", "UserId", "EntityType", "FeatureType" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [EntityType] IS NOT NULL AND [FeatureType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageStatisticsHourly_Period",
                table: "umbracoAiUsageStatisticsHourly",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageStatisticsHourly_Period_ModelId",
                table: "umbracoAiUsageStatisticsHourly",
                columns: new[] { "Period", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageStatisticsHourly_Period_ProfileId",
                table: "umbracoAiUsageStatisticsHourly",
                columns: new[] { "Period", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageStatisticsHourly_Period_ProviderId",
                table: "umbracoAiUsageStatisticsHourly",
                columns: new[] { "Period", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiUsageStatisticsHourly_Period_ProviderId_ModelId_ProfileId_Capability_UserId_EntityType_FeatureType",
                table: "umbracoAiUsageStatisticsHourly",
                columns: new[] { "Period", "ProviderId", "ModelId", "ProfileId", "Capability", "UserId", "EntityType", "FeatureType" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [EntityType] IS NOT NULL AND [FeatureType] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiUsageRecord");

            migrationBuilder.DropTable(
                name: "umbracoAiUsageStatisticsDaily");

            migrationBuilder.DropTable(
                name: "umbracoAiUsageStatisticsHourly");
        }
    }
}
