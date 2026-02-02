using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIAgent_AddVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Version, DateCreated, DateModified columns to UmbracoAIAgent
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "umbracoAIAgent",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "umbracoAIAgent",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModified",
                table: "umbracoAIAgent",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");
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
