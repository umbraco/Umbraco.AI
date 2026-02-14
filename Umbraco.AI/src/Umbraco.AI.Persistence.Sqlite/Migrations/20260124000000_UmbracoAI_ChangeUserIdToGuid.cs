using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_ChangeUserIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite stores GUIDs as TEXT, so we need to drop and recreate the columns
            // umbracoAIConnection
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIConnection");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIConnection");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAIConnection",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAIConnection",
                type: "TEXT",
                nullable: true);

            // umbracoAIProfile
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIProfile");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIProfile");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAIProfile",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAIProfile",
                type: "TEXT",
                nullable: true);

            // umbracoAIContext
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIContext");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIContext");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAIContext",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAIContext",
                type: "TEXT",
                nullable: true);

            // umbracoAIEntityVersion
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIEntityVersion");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAIEntityVersion",
                type: "TEXT",
                nullable: true);

            // umbracoAISettings
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAISettings");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAISettings");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAISettings",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAISettings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // umbracoAIConnection - revert to INTEGER
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIConnection");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIConnection");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIConnection",
                type: "INTEGER",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIConnection",
                type: "INTEGER",
                nullable: true);

            // umbracoAIProfile - revert to INTEGER
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIProfile");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIProfile");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIProfile",
                type: "INTEGER",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIProfile",
                type: "INTEGER",
                nullable: true);

            // umbracoAIContext - revert to INTEGER
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIContext");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIContext");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIContext",
                type: "INTEGER",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIContext",
                type: "INTEGER",
                nullable: true);

            // umbracoAIEntityVersion - revert to INTEGER
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIEntityVersion");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIEntityVersion",
                type: "INTEGER",
                nullable: true);

            // umbracoAISettings - revert to INTEGER
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAISettings");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAISettings");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAISettings",
                type: "INTEGER",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAISettings",
                type: "INTEGER",
                nullable: true);
        }
    }
}
