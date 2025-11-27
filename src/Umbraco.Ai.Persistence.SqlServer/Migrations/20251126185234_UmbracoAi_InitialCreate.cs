using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAiConnection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiConnection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAiProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Capability = table.Column<int>(type: "int", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: true),
                    MaxTokens = table.Column<int>(type: "int", nullable: true),
                    SystemPromptTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiProfile_umbracoAiConnection_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "umbracoAiConnection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiConnection_Alias",
                table: "umbracoAiConnection",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiConnection_ProviderId",
                table: "umbracoAiConnection",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiProfile_Alias",
                table: "umbracoAiProfile",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiProfile_Capability",
                table: "umbracoAiProfile",
                column: "Capability");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiProfile_ConnectionId",
                table: "umbracoAiProfile",
                column: "ConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiProfile");

            migrationBuilder.DropTable(
                name: "umbracoAiConnection");
        }
    }
}
