using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.Sqlite.Migrations
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
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "SystemPromptTemplate",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "umbracoAiProfile");

            // Add versioning and user tracking columns to umbracoAiConnection
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiConnection",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAiConnection",
                type: "INTEGER",
                nullable: true);

            // Add versioning and user tracking columns to umbracoAiProfile
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiProfile",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "umbracoAiProfile",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModified",
                table: "umbracoAiProfile",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAiProfile",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "umbracoAiProfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            // Add version tracking columns to umbracoAiAuditLog
            migrationBuilder.AddColumn<int>(
                name: "ProfileVersion",
                table: "umbracoAiAuditLog",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeatureVersion",
                table: "umbracoAiAuditLog",
                type: "INTEGER",
                nullable: true);

            // Add versioning and user tracking columns to umbracoAiContext
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiContext",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAiContext",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "umbracoAiContext",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            // Create unified umbracoAiEntityVersion table for all entity version history
            migrationBuilder.CreateTable(
                name: "umbracoAiEntityVersion",
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
                    table.PrimaryKey("PK_umbracoAiEntityVersion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiEntityVersion_EntityId_EntityType_Version",
                table: "umbracoAiEntityVersion",
                columns: new[] { "EntityId", "EntityType", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiEntityVersion_EntityType_EntityId",
                table: "umbracoAiEntityVersion",
                columns: new[] { "EntityType", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop unified entity version table
            migrationBuilder.DropTable(
                name: "umbracoAiEntityVersion");

            // Remove versioning and user tracking columns from umbracoAiContext
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "umbracoAiContext");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "umbracoAiContext");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAiContext");

            // Remove versioning and user tracking columns from umbracoAiProfile
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "DateModified",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAiProfile");

            // Remove user tracking columns from umbracoAiConnection
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "umbracoAiConnection");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "umbracoAiConnection");

            // Remove version tracking columns from umbracoAiAuditLog
            migrationBuilder.DropColumn(
                name: "ProfileVersion",
                table: "umbracoAiAuditLog");

            migrationBuilder.DropColumn(
                name: "FeatureVersion",
                table: "umbracoAiAuditLog");

            // Restore deprecated columns to umbracoAiProfile
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

            migrationBuilder.AddColumn<float>(
                name: "Temperature",
                table: "umbracoAiProfile",
                type: "REAL",
                nullable: true);
        }
    }
}
