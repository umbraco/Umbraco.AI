using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIAgent_AddToolPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnabledToolIds",
                table: "umbracoAIAgent",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnabledToolScopeIds",
                table: "umbracoAIAgent",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            // Set default scopes for existing agents (backward compatibility)
            migrationBuilder.Sql(
                @"UPDATE umbracoAIAgent
                  SET EnabledToolScopeIds = '[""search"",""navigation"",""translation"",""web""]'
                  WHERE EnabledToolScopeIds IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnabledToolIds",
                table: "umbracoAIAgent");

            migrationBuilder.DropColumn(
                name: "EnabledToolScopeIds",
                table: "umbracoAIAgent");
        }
    }
}
