using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_ChangeUserIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing int columns and recreate as uniqueidentifier
            // umbracoAIConnection
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIConnection");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIConnection");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAIConnection",
                type: "uniqueidentifier",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAIConnection",
                type: "uniqueidentifier",
                nullable: true);

            // umbracoAIProfile
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIProfile");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIProfile");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAIProfile",
                type: "uniqueidentifier",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAIProfile",
                type: "uniqueidentifier",
                nullable: true);

            // umbracoAIContext
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIContext");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIContext");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAIContext",
                type: "uniqueidentifier",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAIContext",
                type: "uniqueidentifier",
                nullable: true);

            // umbracoAIEntityVersion
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIEntityVersion");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAIEntityVersion",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // umbracoAIConnection - revert to int
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIConnection");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIConnection");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIConnection",
                type: "int",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIConnection",
                type: "int",
                nullable: true);

            // umbracoAIProfile - revert to int
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIProfile");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIProfile");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIProfile",
                type: "int",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIProfile",
                type: "int",
                nullable: true);

            // umbracoAIContext - revert to int
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIContext");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIContext");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIContext",
                type: "int",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIContext",
                type: "int",
                nullable: true);

            // umbracoAIEntityVersion - revert to int
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIEntityVersion");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIEntityVersion",
                type: "int",
                nullable: true);
        }
    }
}
