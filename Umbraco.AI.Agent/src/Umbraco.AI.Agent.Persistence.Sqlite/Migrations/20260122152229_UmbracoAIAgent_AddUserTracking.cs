using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Agent.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAiAgent_AddUserTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIAgent",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIAgent",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "umbracoAIAgent");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "umbracoAIAgent");
        }
    }
}
