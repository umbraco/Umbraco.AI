using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Agent.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAiAgent_ChangeUserIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing int columns and recreate as uniqueidentifier
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "UmbracoAiAgent");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "UmbracoAiAgent");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "UmbracoAiAgent",
                type: "uniqueidentifier",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "UmbracoAiAgent",
                type: "uniqueidentifier",
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
                type: "int",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "UmbracoAiAgent",
                type: "int",
                nullable: true);
        }
    }
}
