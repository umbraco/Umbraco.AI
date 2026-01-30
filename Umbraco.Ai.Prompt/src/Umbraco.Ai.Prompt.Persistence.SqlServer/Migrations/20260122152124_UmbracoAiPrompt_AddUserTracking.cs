using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Prompt.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAiPrompt_AddUserTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiPrompt",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAiPrompt",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "umbracoAiPrompt");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "umbracoAiPrompt");
        }
    }
}
