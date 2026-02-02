using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.SqlServer.Migrations
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
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove Version column from umbracoAiConnection
            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAIConnection");
        }
    }
}
