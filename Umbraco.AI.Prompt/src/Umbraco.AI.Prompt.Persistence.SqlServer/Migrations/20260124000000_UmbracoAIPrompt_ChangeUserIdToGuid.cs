using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Prompt.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAiPrompt_ChangeUserIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing int columns and recreate as uniqueidentifier
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiPrompt");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiPrompt");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAiPrompt",
                type: "uniqueidentifier",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAiPrompt",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiPrompt");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiPrompt");
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
    }
}
