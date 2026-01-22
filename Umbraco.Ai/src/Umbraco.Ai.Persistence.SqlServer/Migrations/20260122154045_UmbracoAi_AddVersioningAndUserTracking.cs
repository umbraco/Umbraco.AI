using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddVersioningAndUserTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove deprecated columns from umbracoAiProfile
            migrationBuilder.DropColumn(
                name: "MaxTokens",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "SystemPromptTemplate",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "umbracoAiProfile");

            // Add versioning and user tracking columns to umbracoAiConnection
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiConnection",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAiConnection",
                type: "int",
                nullable: true);

            // Add versioning and user tracking columns to umbracoAiProfile
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiProfile",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "umbracoAiProfile",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModified",
                table: "umbracoAiProfile",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAiProfile",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "umbracoAiProfile",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Add version tracking columns to umbracoAiAuditLog
            migrationBuilder.AddColumn<int>(
                name: "ProfileVersion",
                table: "umbracoAiAuditLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeatureVersion",
                table: "umbracoAiAuditLog",
                type: "int",
                nullable: true);

            // Add versioning and user tracking columns to umbracoAiContext
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiContext",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAiContext",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "umbracoAiContext",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Create umbracoAiProfileVersion table
            migrationBuilder.CreateTable(
                name: "umbracoAiProfileVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ChangeDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
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
                name: "IX_umbracoAiProfileVersion_ProfileId",
                table: "umbracoAiProfileVersion",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiProfileVersion_ProfileId_Version",
                table: "umbracoAiProfileVersion",
                columns: new[] { "ProfileId", "Version" },
                unique: true);

            // Create umbracoAiContextVersion table
            migrationBuilder.CreateTable(
                name: "umbracoAiContextVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContextId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ChangeDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiContextVersion_ContextId",
                table: "umbracoAiContextVersion",
                column: "ContextId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiContextVersion_ContextId_Version",
                table: "umbracoAiContextVersion",
                columns: new[] { "ContextId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop version tables
            migrationBuilder.DropTable(
                name: "umbracoAiContextVersion");

            migrationBuilder.DropTable(
                name: "umbracoAiProfileVersion");

            // Remove versioning and user tracking columns from umbracoAiContext
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "umbracoAiContext");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "umbracoAiContext");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAiContext");

            // Remove versioning and user tracking columns from umbracoAiProfile
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "DateModified",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "umbracoAiProfile");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "umbracoAiProfile");

            // Remove user tracking columns from umbracoAiConnection
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "umbracoAiConnection");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "umbracoAiConnection");

            // Remove version tracking columns from umbracoAiAuditLog
            migrationBuilder.DropColumn(
                name: "ProfileVersion",
                table: "umbracoAiAuditLog");

            migrationBuilder.DropColumn(
                name: "FeatureVersion",
                table: "umbracoAiAuditLog");

            // Restore deprecated columns to umbracoAiProfile
            migrationBuilder.AddColumn<int>(
                name: "MaxTokens",
                table: "umbracoAiProfile",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemPromptTemplate",
                table: "umbracoAiProfile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Temperature",
                table: "umbracoAiProfile",
                type: "real",
                nullable: true);
        }
    }
}
