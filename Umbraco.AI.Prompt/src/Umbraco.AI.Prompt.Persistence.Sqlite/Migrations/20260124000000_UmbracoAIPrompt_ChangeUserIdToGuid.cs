using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Prompt.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAiPrompt_ChangeUserIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite stores GUIDs as TEXT, so we need to drop and recreate the columns
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIPrompt");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIPrompt");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAIPrompt",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAIPrompt",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIPrompt");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIPrompt");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIPrompt",
                type: "INTEGER",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIPrompt",
                type: "INTEGER",
                nullable: true);
        }
    }
}
