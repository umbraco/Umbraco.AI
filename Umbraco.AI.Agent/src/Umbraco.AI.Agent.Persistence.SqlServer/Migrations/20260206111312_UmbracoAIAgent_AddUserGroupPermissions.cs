using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIAgent_AddUserGroupPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserGroupPermissions",
                table: "umbracoAIAgent",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserGroupPermissions",
                table: "umbracoAIAgent");
        }
    }
}
