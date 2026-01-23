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

            // Create umbracoAiConnectionVersion table
            migrationBuilder.CreateTable(
                name: "umbracoAiConnectionVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ChangeDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiConnectionVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiConnectionVersion_umbracoAiConnection_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "umbracoAiConnection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiConnectionVersion_ConnectionId",
                table: "umbracoAiConnectionVersion",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiConnectionVersion_ConnectionId_Version",
                table: "umbracoAiConnectionVersion",
                columns: new[] { "ConnectionId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop version table
            migrationBuilder.DropTable(
                name: "umbracoAiConnectionVersion");

            // Remove Version column from umbracoAiConnection
            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAiConnection");
        }
    }
}
