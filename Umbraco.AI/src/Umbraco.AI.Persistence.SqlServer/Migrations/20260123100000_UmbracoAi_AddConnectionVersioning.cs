using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddConnectionVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Version column to umbracoAiConnection table
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "umbracoAiConnection",
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
                table: "umbracoAiConnection");
        }
    }
}
