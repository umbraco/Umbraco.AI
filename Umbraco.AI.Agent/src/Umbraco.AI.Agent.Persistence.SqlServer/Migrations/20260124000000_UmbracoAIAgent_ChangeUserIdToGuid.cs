using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIAgent_ChangeUserIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing int columns and recreate as uniqueidentifier
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIAgent");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIAgent");
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "umbracoAIAgent",
                type: "uniqueidentifier",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "umbracoAIAgent",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "umbracoAIAgent");
            migrationBuilder.DropColumn(name: "ModifiedByUserId", table: "umbracoAIAgent");
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "umbracoAIAgent",
                type: "int",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "umbracoAIAgent",
                type: "int",
                nullable: true);
        }
    }
}
