using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_ChangeUserIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing int columns and recreate as uniqueidentifier
            // umbracoAiConnection
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiConnection");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiConnection");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAiConnection",
                type: "uniqueidentifier",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAiConnection",
                type: "uniqueidentifier",
                nullable: true);

            // umbracoAiProfile
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiProfile");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiProfile");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAiProfile",
                type: "uniqueidentifier",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAiProfile",
                type: "uniqueidentifier",
                nullable: true);

            // umbracoAiContext
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiContext");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiContext");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAiContext",
                type: "uniqueidentifier",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAiContext",
                type: "uniqueidentifier",
                nullable: true);

            // umbracoAiEntityVersion
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiEntityVersion");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAiEntityVersion",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // umbracoAiConnection - revert to int
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiConnection");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiConnection");
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

            // umbracoAiProfile - revert to int
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiProfile");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiProfile");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiProfile",
                type: "int",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAiProfile",
                type: "int",
                nullable: true);

            // umbracoAiContext - revert to int
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiContext");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAiContext");
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

            // umbracoAiEntityVersion - revert to int
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAiEntityVersion");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAiEntityVersion",
                type: "int",
                nullable: true);
        }
    }
}
