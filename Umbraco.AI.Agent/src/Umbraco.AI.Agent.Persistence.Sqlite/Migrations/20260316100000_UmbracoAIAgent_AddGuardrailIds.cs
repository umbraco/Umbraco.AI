using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIAgent_AddGuardrailIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add GuardrailIds column at agent level (available for all agent types)
            migrationBuilder.AddColumn<string>(
                name: "GuardrailIds",
                table: "umbracoAIAgent",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            // 2. Migrate guardrailIds from Config JSON blob to the new column (standard agents only)
            migrationBuilder.Sql("""
                UPDATE umbracoAIAgent
                SET GuardrailIds = json_extract(Config, '$.guardrailIds')
                WHERE AgentType = 0
                    AND Config IS NOT NULL
                    AND json_extract(Config, '$.guardrailIds') IS NOT NULL
                    AND json_extract(Config, '$.guardrailIds') <> '[]'
                """);

            // 3. Remove guardrailIds from Config JSON blob
            migrationBuilder.Sql("""
                UPDATE umbracoAIAgent
                SET Config = json_remove(Config, '$.guardrailIds')
                WHERE AgentType = 0
                    AND Config IS NOT NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore guardrailIds into Config JSON for standard agents
            migrationBuilder.Sql("""
                UPDATE umbracoAIAgent
                SET Config = json_set(Config, '$.guardrailIds', json(GuardrailIds))
                WHERE AgentType = 0
                    AND Config IS NOT NULL
                    AND GuardrailIds IS NOT NULL
                """);

            migrationBuilder.DropColumn(
                name: "GuardrailIds",
                table: "umbracoAIAgent");
        }
    }
}
