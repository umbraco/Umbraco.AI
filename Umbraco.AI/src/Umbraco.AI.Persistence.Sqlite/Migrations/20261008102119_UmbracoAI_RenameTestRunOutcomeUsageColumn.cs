using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_RenameTestRunOutcomeUsageColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OutcomeTokenUsageJson",
                table: "umbracoAITestRun",
                newName: "OutcomeUsageJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OutcomeUsageJson",
                table: "umbracoAITestRun",
                newName: "OutcomeTokenUsageJson");
        }
    }
}
