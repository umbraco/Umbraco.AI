using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_AddUsageAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAIUsageRecord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Capability = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileAlias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FeatureType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FeatureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    InputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIUsageRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAIUsageStatisticsDaily",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Capability = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RequestCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailureCount = table.Column<int>(type: "INTEGER", nullable: false),
                    InputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalDurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIUsageStatisticsDaily", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAIUsageStatisticsHourly",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Period = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Capability = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RequestCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailureCount = table.Column<int>(type: "INTEGER", nullable: false),
                    InputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalDurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIUsageStatisticsHourly", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageRecord_Timestamp",
                table: "umbracoAIUsageRecord",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageRecord_Timestamp_Status",
                table: "umbracoAIUsageRecord",
                columns: new[] { "Timestamp", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageStatisticsDaily_Period",
                table: "umbracoAIUsageStatisticsDaily",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageStatisticsDaily_Period_ModelId",
                table: "umbracoAIUsageStatisticsDaily",
                columns: new[] { "Period", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageStatisticsDaily_Period_ProfileId",
                table: "umbracoAIUsageStatisticsDaily",
                columns: new[] { "Period", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageStatisticsDaily_Period_ProviderId",
                table: "umbracoAIUsageStatisticsDaily",
                columns: new[] { "Period", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageStatisticsDaily_Period_ProviderId_ModelId_ProfileId_Capability_UserId_EntityType_FeatureType",
                table: "umbracoAIUsageStatisticsDaily",
                columns: new[] { "Period", "ProviderId", "ModelId", "ProfileId", "Capability", "UserId", "EntityType", "FeatureType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageStatisticsHourly_Period",
                table: "umbracoAIUsageStatisticsHourly",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageStatisticsHourly_Period_ModelId",
                table: "umbracoAIUsageStatisticsHourly",
                columns: new[] { "Period", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageStatisticsHourly_Period_ProfileId",
                table: "umbracoAIUsageStatisticsHourly",
                columns: new[] { "Period", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageStatisticsHourly_Period_ProviderId",
                table: "umbracoAIUsageStatisticsHourly",
                columns: new[] { "Period", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIUsageStatisticsHourly_Period_ProviderId_ModelId_ProfileId_Capability_UserId_EntityType_FeatureType",
                table: "umbracoAIUsageStatisticsHourly",
                columns: new[] { "Period", "ProviderId", "ModelId", "ProfileId", "Capability", "UserId", "EntityType", "FeatureType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAIUsageRecord");

            migrationBuilder.DropTable(
                name: "umbracoAIUsageStatisticsDaily");

            migrationBuilder.DropTable(
                name: "umbracoAIUsageStatisticsHourly");
        }
    }
}
