using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIAgent_AddStarterPrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StarterPrompts",
                table: "umbracoAIAgent",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StarterPrompts",
                table: "umbracoAIAgent");
        }
    }
}
