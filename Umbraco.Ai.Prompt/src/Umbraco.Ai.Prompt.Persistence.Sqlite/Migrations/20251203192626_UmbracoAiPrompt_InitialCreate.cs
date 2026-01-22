using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Prompt.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAiPrompt_InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAiPrompt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Instructions = table.Column<string>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiPrompt", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiPrompt_Alias",
                table: "umbracoAiPrompt",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiPrompt_ProfileId",
                table: "umbracoAiPrompt",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiPrompt");
        }
    }
}
