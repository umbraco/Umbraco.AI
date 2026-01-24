using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Agent.Persistence.SqlServer.Migrations
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
                table: "UmbracoAiAgent",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "UmbracoAiAgent",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModified",
                table: "UmbracoAiAgent",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "UmbracoAiAgent");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "UmbracoAiAgent");

            migrationBuilder.DropColumn(
                name: "DateModified",
                table: "UmbracoAiAgent");
        }
    }
}
