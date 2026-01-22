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
                table: "UmbracoAiAgent",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "UmbracoAiAgent",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModified",
                table: "UmbracoAiAgent",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            // Create umbracoAiAgentVersion table
            migrationBuilder.CreateTable(
                name: "umbracoAiAgentVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChangeDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiAgentVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiAgentVersion_UmbracoAiAgent_AgentId",
                        column: x => x.AgentId,
                        principalTable: "UmbracoAiAgent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAgentVersion_AgentId",
                table: "umbracoAiAgentVersion",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiAgentVersion_AgentId_Version",
                table: "umbracoAiAgentVersion",
                columns: new[] { "AgentId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiAgentVersion");

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
