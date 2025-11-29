using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_ProfileSettingsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add the new SettingsJson column
            migrationBuilder.AddColumn<string>(
                name: "SettingsJson",
                table: "umbracoAiProfile",
                type: "nvarchar(max)",
                nullable: true);

            // Step 2: Migrate existing data - convert Temperature, MaxTokens, SystemPromptTemplate to JSON
            // Only migrate Chat profiles (Capability = 1) that have any settings
            migrationBuilder.Sql(@"
                UPDATE umbracoAiProfile
                SET SettingsJson = (
                    SELECT
                        CASE WHEN Temperature IS NOT NULL THEN Temperature END AS temperature,
                        CASE WHEN MaxTokens IS NOT NULL THEN MaxTokens END AS maxTokens,
                        CASE WHEN SystemPromptTemplate IS NOT NULL THEN SystemPromptTemplate END AS systemPromptTemplate
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                )
                WHERE Capability = 1
                  AND (Temperature IS NOT NULL OR MaxTokens IS NOT NULL OR SystemPromptTemplate IS NOT NULL)
            ");

            // Step 3: Clean up empty JSON objects (when all values were NULL)
            migrationBuilder.Sql(@"
                UPDATE umbracoAiProfile
                SET SettingsJson = NULL
                WHERE SettingsJson = '{}'
            ");

            // Step 4: Drop the old columns
            migrationBuilder.DropColumn(
                name: "MaxTokens",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "SystemPromptTemplate",
                table: "umbracoAiProfile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add back the old columns
            migrationBuilder.AddColumn<float>(
                name: "Temperature",
                table: "umbracoAiProfile",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxTokens",
                table: "umbracoAiProfile",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemPromptTemplate",
                table: "umbracoAiProfile",
                type: "nvarchar(max)",
                nullable: true);

            // Step 2: Migrate data back from JSON
            migrationBuilder.Sql(@"
                UPDATE umbracoAiProfile
                SET
                    Temperature = JSON_VALUE(SettingsJson, '$.temperature'),
                    MaxTokens = JSON_VALUE(SettingsJson, '$.maxTokens'),
                    SystemPromptTemplate = JSON_VALUE(SettingsJson, '$.systemPromptTemplate')
                WHERE SettingsJson IS NOT NULL
            ");

            // Step 3: Drop the SettingsJson column
            migrationBuilder.DropColumn(
                name: "SettingsJson",
                table: "umbracoAiProfile");
        }
    }
}
