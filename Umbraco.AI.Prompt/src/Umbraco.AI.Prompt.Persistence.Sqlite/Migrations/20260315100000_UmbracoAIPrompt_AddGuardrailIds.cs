using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Prompt.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIPrompt_AddGuardrailIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuardrailIds",
                table: "umbracoAIPrompt",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuardrailIds",
                table: "umbracoAIPrompt");
        }
    }
}
