using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_ChangeUserIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite stores GUIDs as TEXT, so we need to drop and recreate the columns
            // umbracoAiConnection
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiConnection");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiConnection");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAiConnection",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAiConnection",
                type: "TEXT",
                nullable: true);

            // umbracoAiProfile
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiProfile");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiProfile");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAiProfile",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAiProfile",
                type: "TEXT",
                nullable: true);

            // umbracoAiContext
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiContext");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiContext");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAiContext",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAiContext",
                type: "TEXT",
                nullable: true);

            // umbracoAiEntityVersion
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiEntityVersion");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAiEntityVersion",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // umbracoAiConnection - revert to INTEGER
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiConnection");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiConnection");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiConnection",
                type: "INTEGER",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAiConnection",
                type: "INTEGER",
                nullable: true);

            // umbracoAiProfile - revert to INTEGER
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiProfile");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiProfile");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiProfile",
                type: "INTEGER",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAiProfile",
                type: "INTEGER",
                nullable: true);

            // umbracoAiContext - revert to INTEGER
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiContext");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiContext");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiContext",
                type: "INTEGER",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAiContext",
                type: "INTEGER",
                nullable: true);

            // umbracoAiEntityVersion - revert to INTEGER
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiEntityVersion");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiEntityVersion",
                type: "INTEGER",
                nullable: true);
        }
    }
}
