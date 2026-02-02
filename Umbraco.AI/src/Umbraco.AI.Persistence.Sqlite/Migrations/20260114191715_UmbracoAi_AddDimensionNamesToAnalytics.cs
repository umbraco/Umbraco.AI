using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddDimensionNamesToAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileAlias",
                table: "umbracoAIUsageStatisticsHourly",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "umbracoAIUsageStatisticsHourly",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileAlias",
                table: "umbracoAIUsageStatisticsDaily",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "umbracoAIUsageStatisticsDaily",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileAlias",
                table: "umbracoAIUsageStatisticsHourly");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "umbracoAIUsageStatisticsHourly");

            migrationBuilder.DropColumn(
                name: "ProfileAlias",
                table: "umbracoAIUsageStatisticsDaily");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "umbracoAIUsageStatisticsDaily");
        }
    }
}
