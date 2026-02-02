using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Prompt.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAiPrompt_AddVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Version column to umbracoAiPrompt
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "umbracoAiPrompt",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAiPrompt");
        }
    }
}
