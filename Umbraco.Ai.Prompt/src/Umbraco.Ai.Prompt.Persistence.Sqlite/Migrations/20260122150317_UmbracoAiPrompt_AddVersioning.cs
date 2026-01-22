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

            // Create umbracoAiPromptVersion table
            migrationBuilder.CreateTable(
                name: "umbracoAiPromptVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PromptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChangeDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiPromptVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiPromptVersion_umbracoAiPrompt_PromptId",
                        column: x => x.PromptId,
                        principalTable: "umbracoAiPrompt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiPromptVersion_PromptId",
                table: "umbracoAiPromptVersion",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiPromptVersion_PromptId_Version",
                table: "umbracoAiPromptVersion",
                columns: new[] { "PromptId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiPromptVersion");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAiPrompt");
        }
    }
}
