using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddAiContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContextIds",
                table: "umbracoAiProfile",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "umbracoAiContext",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiContext", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAiContextResource",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContextId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ResourceTypeId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: false),
                    InjectionMode = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiContextResource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiContextResource_umbracoAiContext_ContextId",
                        column: x => x.ContextId,
                        principalTable: "umbracoAiContext",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiContext_Alias",
                table: "umbracoAiContext",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiContextResource_ContextId",
                table: "umbracoAiContextResource",
                column: "ContextId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiContextResource_ResourceTypeId",
                table: "umbracoAiContextResource",
                column: "ResourceTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiContextResource");

            migrationBuilder.DropTable(
                name: "umbracoAiContext");

            migrationBuilder.DropColumn(
                name: "ContextIds",
                table: "umbracoAiProfile");
        }
    }
}
