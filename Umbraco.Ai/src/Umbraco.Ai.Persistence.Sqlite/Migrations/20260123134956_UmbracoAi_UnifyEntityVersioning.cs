using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_UnifyEntityVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiConnectionVersion");

            migrationBuilder.DropTable(
                name: "umbracoAiContextVersion");

            migrationBuilder.DropTable(
                name: "umbracoAiProfileVersion");

            migrationBuilder.CreateTable(
                name: "umbracoAiEntityVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChangeDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiEntityVersion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiEntityVersion_EntityId_EntityType_Version",
                table: "umbracoAiEntityVersion",
                columns: new[] { "EntityId", "EntityType", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiEntityVersion_EntityType_EntityId",
                table: "umbracoAiEntityVersion",
                columns: new[] { "EntityType", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiEntityVersion");

            migrationBuilder.CreateTable(
                name: "umbracoAiConnectionVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChangeDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ConnectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "umbracoAiContextVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChangeDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ContextId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiContextVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiContextVersion_umbracoAiContext_ContextId",
                        column: x => x.ContextId,
                        principalTable: "umbracoAiContext",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAiProfileVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChangeDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiProfileVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiProfileVersion_umbracoAiProfile_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "umbracoAiProfile",
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

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiContextVersion_ContextId",
                table: "umbracoAiContextVersion",
                column: "ContextId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiContextVersion_ContextId_Version",
                table: "umbracoAiContextVersion",
                columns: new[] { "ContextId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiProfileVersion_ProfileId",
                table: "umbracoAiProfileVersion",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiProfileVersion_ProfileId_Version",
                table: "umbracoAiProfileVersion",
                columns: new[] { "ProfileId", "Version" },
                unique: true);
        }
    }
}
