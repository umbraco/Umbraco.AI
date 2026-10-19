using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_AddUsageNestedRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NestedRequestCount",
                table: "umbracoAIUsageStatisticsHourly",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NestedRequestCount",
                table: "umbracoAIUsageStatisticsDaily",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsNested",
                table: "umbracoAIUsageRecord",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NestedRequestCount",
                table: "umbracoAIUsageStatisticsHourly");

            migrationBuilder.DropColumn(
                name: "NestedRequestCount",
                table: "umbracoAIUsageStatisticsDaily");

            migrationBuilder.DropColumn(
                name: "IsNested",
                table: "umbracoAIUsageRecord");
        }
    }
}
