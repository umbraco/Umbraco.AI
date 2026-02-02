using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_AddConnectionVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Version column to umbracoAIConnection table
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "umbracoAIConnection",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove Version column from umbracoAIConnection
            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAIConnection");
        }
    }
}
