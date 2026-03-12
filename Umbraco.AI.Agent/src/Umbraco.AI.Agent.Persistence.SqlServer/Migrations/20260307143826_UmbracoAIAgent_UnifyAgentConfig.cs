using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIAgent_UnifyAgentConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add new columns
            migrationBuilder.AddColumn<int>(
                name: "AgentType",
                table: "umbracoAIAgent",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Config",
                table: "umbracoAIAgent",
                type: "nvarchar(max)",
                nullable: true);

            // 2. Migrate existing standard agent data into Config JSON blob
            migrationBuilder.Sql("""
                UPDATE umbracoAIAgent
                SET Config = '{'
                    + '"contextIds":' + COALESCE(ContextIds, '[]')
                    + ',"instructions":' + CASE WHEN Instructions IS NULL THEN 'null' ELSE '"' + STRING_ESCAPE(Instructions, 'json') + '"' END
                    + ',"allowedToolIds":' + COALESCE(AllowedToolIds, '[]')
                    + ',"allowedToolScopeIds":' + COALESCE(AllowedToolScopeIds, '[]')
                    + ',"userGroupPermissions":' + COALESCE(UserGroupPermissions, '{}')
                    + '}'
                """);

            // 3. Drop old columns that are now in Config
            migrationBuilder.DropColumn(
                name: "AllowedToolIds",
                table: "umbracoAIAgent");

            migrationBuilder.DropColumn(
                name: "AllowedToolScopeIds",
                table: "umbracoAIAgent");

            migrationBuilder.DropColumn(
                name: "ContextIds",
                table: "umbracoAIAgent");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "umbracoAIAgent");

            migrationBuilder.DropColumn(
                name: "UserGroupPermissions",
                table: "umbracoAIAgent");

            // 4. Add index on AgentType
            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAgent_AgentType",
                table: "umbracoAIAgent",
                column: "AgentType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_umbracoAIAgent_AgentType",
                table: "umbracoAIAgent");

            migrationBuilder.DropColumn(
                name: "AgentType",
                table: "umbracoAIAgent");

            migrationBuilder.DropColumn(
                name: "Config",
                table: "umbracoAIAgent");

            migrationBuilder.AddColumn<string>(
                name: "AllowedToolIds",
                table: "umbracoAIAgent",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedToolScopeIds",
                table: "umbracoAIAgent",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextIds",
                table: "umbracoAIAgent",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "umbracoAIAgent",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserGroupPermissions",
                table: "umbracoAIAgent",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
