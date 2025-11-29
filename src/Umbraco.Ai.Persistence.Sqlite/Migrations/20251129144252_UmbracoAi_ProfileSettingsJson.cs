using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.Sqlite.Migrations
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
                type: "TEXT",
                nullable: true);

            // Step 2: Migrate existing data - convert Temperature, MaxTokens, SystemPromptTemplate to JSON
            // Only migrate Chat profiles (Capability = 1) that have any settings
            // SQLite uses json_object() for building JSON
            migrationBuilder.Sql(@"
                UPDATE umbracoAiProfile
                SET SettingsJson = json_object(
                    'temperature', Temperature,
                    'maxTokens', MaxTokens,
                    'systemPromptTemplate', SystemPromptTemplate
                )
                WHERE Capability = 1
                  AND (Temperature IS NOT NULL OR MaxTokens IS NOT NULL OR SystemPromptTemplate IS NOT NULL)
            ");

            // Step 3: Remove null keys from JSON (SQLite json_object includes all keys even if null)
            // We need to rebuild the JSON without null values
            migrationBuilder.Sql(@"
                UPDATE umbracoAiProfile
                SET SettingsJson = (
                    SELECT json_group_object(key, value)
                    FROM json_each(SettingsJson)
                    WHERE value IS NOT NULL
                )
                WHERE SettingsJson IS NOT NULL
            ");

            // Step 4: Clean up empty JSON objects
            migrationBuilder.Sql(@"
                UPDATE umbracoAiProfile
                SET SettingsJson = NULL
                WHERE SettingsJson = '{}'
            ");

            // Step 5: Drop the old columns
            // Note: SQLite doesn't support DROP COLUMN directly in older versions,
            // but EF Core handles this via table rebuild
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
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxTokens",
                table: "umbracoAiProfile",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemPromptTemplate",
                table: "umbracoAiProfile",
                type: "TEXT",
                nullable: true);

            // Step 2: Migrate data back from JSON using json_extract
            migrationBuilder.Sql(@"
                UPDATE umbracoAiProfile
                SET
                    Temperature = json_extract(SettingsJson, '$.temperature'),
                    MaxTokens = json_extract(SettingsJson, '$.maxTokens'),
                    SystemPromptTemplate = json_extract(SettingsJson, '$.systemPromptTemplate')
                WHERE SettingsJson IS NOT NULL
            ");

            // Step 3: Drop the SettingsJson column
            migrationBuilder.DropColumn(
                name: "SettingsJson",
                table: "umbracoAiProfile");
        }
    }
}
