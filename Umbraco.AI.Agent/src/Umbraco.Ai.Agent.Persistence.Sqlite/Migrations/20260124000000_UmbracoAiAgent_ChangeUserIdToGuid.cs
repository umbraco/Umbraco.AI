using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Agent.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAiAgent_ChangeUserIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite stores GUIDs as TEXT, so we need to drop and recreate the columns
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "UmbracoAiAgent");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "UmbracoAiAgent");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "UmbracoAiAgent",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "UmbracoAiAgent",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "UmbracoAiAgent");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "UmbracoAiAgent");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "UmbracoAiAgent",
                type: "INTEGER",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "UmbracoAiAgent",
                type: "INTEGER",
                nullable: true);
        }
    }
}
