using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddDimensionNamesToAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileAlias",
                table: "umbracoAiUsageStatisticsHourly",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "umbracoAiUsageStatisticsHourly",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileAlias",
                table: "umbracoAiUsageStatisticsDaily",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "umbracoAiUsageStatisticsDaily",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileAlias",
                table: "umbracoAiUsageStatisticsHourly");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "umbracoAiUsageStatisticsHourly");

            migrationBuilder.DropColumn(
                name: "ProfileAlias",
                table: "umbracoAiUsageStatisticsDaily");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "umbracoAiUsageStatisticsDaily");
        }
    }
}
