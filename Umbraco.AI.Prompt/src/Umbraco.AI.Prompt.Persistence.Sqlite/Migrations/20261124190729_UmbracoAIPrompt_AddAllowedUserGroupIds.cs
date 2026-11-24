using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Prompt.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIPrompt_AddAllowedUserGroupIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedUserGroupIds",
                table: "umbracoAIPrompt",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedUserGroupIds",
                table: "umbracoAIPrompt");
        }
    }
}
