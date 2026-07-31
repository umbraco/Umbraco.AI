using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_AddCachedInputTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CachedInputTokens",
                table: "umbracoAIUsageStatisticsHourly",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CachedInputTokens",
                table: "umbracoAIUsageStatisticsDaily",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CachedInputTokens",
                table: "umbracoAIUsageRecord",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CachedInputTokens",
                table: "umbracoAIAuditLog",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CachedInputTokens",
                table: "umbracoAIUsageStatisticsHourly");

            migrationBuilder.DropColumn(
                name: "CachedInputTokens",
                table: "umbracoAIUsageStatisticsDaily");

            migrationBuilder.DropColumn(
                name: "CachedInputTokens",
                table: "umbracoAIUsageRecord");

            migrationBuilder.DropColumn(
                name: "CachedInputTokens",
                table: "umbracoAIAuditLog");
        }
    }
}
