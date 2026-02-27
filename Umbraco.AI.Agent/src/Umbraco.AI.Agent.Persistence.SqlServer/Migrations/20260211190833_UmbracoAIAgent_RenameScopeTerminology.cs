using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIAgent_RenameScopeTerminology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScopeIds",
                table: "umbracoAIAgent",
                newName: "SurfaceIds");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "umbracoAIAgent",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scope",
                table: "umbracoAIAgent");

            migrationBuilder.RenameColumn(
                name: "SurfaceIds",
                table: "umbracoAIAgent",
                newName: "ScopeIds");
        }
    }
}
