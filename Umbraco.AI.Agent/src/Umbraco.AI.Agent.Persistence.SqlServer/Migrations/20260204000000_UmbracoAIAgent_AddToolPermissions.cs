using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIAgent_AddToolPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedToolIds",
                table: "umbracoAIAgent",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedToolScopeIds",
                table: "umbracoAIAgent",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedToolIds",
                table: "umbracoAIAgent");

            migrationBuilder.DropColumn(
                name: "AllowedToolScopeIds",
                table: "umbracoAIAgent");
        }
    }
}
