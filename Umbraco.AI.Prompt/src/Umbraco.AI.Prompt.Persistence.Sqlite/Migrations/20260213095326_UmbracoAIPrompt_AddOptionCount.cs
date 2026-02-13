using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Prompt.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIPrompt_AddOptionCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OptionCount",
                table: "umbracoAIPrompt",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OptionCount",
                table: "umbracoAIPrompt");
        }
    }
}
