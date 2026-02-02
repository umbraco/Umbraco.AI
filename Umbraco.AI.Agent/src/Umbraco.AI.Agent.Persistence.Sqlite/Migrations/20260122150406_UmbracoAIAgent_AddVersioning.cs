using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Agent.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAiAgent_AddVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Version, DateCreated, DateModified columns to UmbracoAiAgent
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "umbracoAIAgent",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "umbracoAIAgent",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModified",
                table: "umbracoAIAgent",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAIAgent");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "umbracoAIAgent");

            migrationBuilder.DropColumn(
                name: "DateModified",
                table: "umbracoAIAgent");
        }
    }
}
