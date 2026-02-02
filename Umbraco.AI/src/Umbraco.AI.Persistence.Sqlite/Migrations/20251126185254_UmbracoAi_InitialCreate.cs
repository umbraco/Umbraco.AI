using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAIConnection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Settings = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIConnection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAIProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Capability = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ConnectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Temperature = table.Column<float>(type: "REAL", nullable: true),
                    MaxTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    SystemPromptTemplate = table.Column<string>(type: "TEXT", nullable: true),
                    Settings = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAIProfile_umbracoAiConnection_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "umbracoAIConnection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIConnection_Alias",
                table: "umbracoAIConnection",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIConnection_ProviderId",
                table: "umbracoAIConnection",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIProfile_Alias",
                table: "umbracoAIProfile",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIProfile_Capability",
                table: "umbracoAIProfile",
                column: "Capability");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIProfile_ConnectionId",
                table: "umbracoAIProfile",
                column: "ConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAIProfile");

            migrationBuilder.DropTable(
                name: "umbracoAIConnection");
        }
    }
}
