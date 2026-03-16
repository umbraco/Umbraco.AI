using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_AddTraceIdToAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraceId",
                table: "umbracoAIAuditLog",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAuditLog_TraceId",
                table: "umbracoAIAuditLog",
                column: "TraceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_umbracoAIAuditLog_TraceId",
                table: "umbracoAIAuditLog");

            migrationBuilder.DropColumn(
                name: "TraceId",
                table: "umbracoAIAuditLog");
        }
    }
}
