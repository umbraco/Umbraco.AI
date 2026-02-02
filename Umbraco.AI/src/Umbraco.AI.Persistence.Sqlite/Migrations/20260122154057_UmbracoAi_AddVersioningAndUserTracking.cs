using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddVersioningAndUserTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove deprecated columns from umbracoAiProfile
            migrationBuilder.DropColumn(
                name: "MaxTokens",
                table: "umbracoAIProfile");

            migrationBuilder.DropColumn(
                name: "SystemPromptTemplate",
                table: "umbracoAIProfile");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "umbracoAIProfile");

            // Add versioning and user tracking columns to umbracoAiConnection
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIConnection",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIConnection",
                type: "INTEGER",
                nullable: true);

            // Add versioning and user tracking columns to umbracoAiProfile
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIProfile",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "umbracoAIProfile",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModified",
                table: "umbracoAIProfile",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIProfile",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "umbracoAIProfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            // Add version tracking columns to umbracoAiAuditLog
            migrationBuilder.AddColumn<int>(
                name: "ProfileVersion",
                table: "umbracoAIAuditLog",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeatureVersion",
                table: "umbracoAIAuditLog",
                type: "INTEGER",
                nullable: true);

            // Add versioning and user tracking columns to umbracoAiContext
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIContext",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIContext",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "umbracoAIContext",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            // Create unified umbracoAiEntityVersion table for all entity version history
            migrationBuilder.CreateTable(
                name: "umbracoAIEntityVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChangeDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIEntityVersion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIEntityVersion_EntityId_EntityType_Version",
                table: "umbracoAIEntityVersion",
                columns: new[] { "EntityId", "EntityType", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIEntityVersion_EntityType_EntityId",
                table: "umbracoAIEntityVersion",
                columns: new[] { "EntityType", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop unified entity version table
            migrationBuilder.DropTable(
                name: "umbracoAIEntityVersion");

            // Remove versioning and user tracking columns from umbracoAiContext
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "umbracoAIContext");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "umbracoAIContext");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAIContext");

            // Remove versioning and user tracking columns from umbracoAiProfile
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "umbracoAIProfile");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "umbracoAIProfile");

            migrationBuilder.DropColumn(
                name: "DateModified",
                table: "umbracoAIProfile");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "umbracoAIProfile");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAIProfile");

            // Remove user tracking columns from umbracoAiConnection
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "umbracoAIConnection");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "umbracoAIConnection");

            // Remove version tracking columns from umbracoAiAuditLog
            migrationBuilder.DropColumn(
                name: "ProfileVersion",
                table: "umbracoAIAuditLog");

            migrationBuilder.DropColumn(
                name: "FeatureVersion",
                table: "umbracoAIAuditLog");

            // Restore deprecated columns to umbracoAiProfile
            migrationBuilder.AddColumn<int>(
                name: "MaxTokens",
                table: "umbracoAIProfile",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemPromptTemplate",
                table: "umbracoAIProfile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Temperature",
                table: "umbracoAIProfile",
                type: "REAL",
                nullable: true);
        }
    }
}
