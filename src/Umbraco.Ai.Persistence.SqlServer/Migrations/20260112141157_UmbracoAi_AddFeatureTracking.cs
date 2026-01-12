using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddFeatureTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FeatureId",
                table: "umbracoAiTrace",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatureType",
                table: "umbracoAiTrace",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTrace_FeatureId",
                table: "umbracoAiTrace",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTrace_FeatureType_FeatureId",
                table: "umbracoAiTrace",
                columns: new[] { "FeatureType", "FeatureId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_umbracoAiTrace_FeatureId",
                table: "umbracoAiTrace");

            migrationBuilder.DropIndex(
                name: "IX_umbracoAiTrace_FeatureType_FeatureId",
                table: "umbracoAiTrace");

            migrationBuilder.DropColumn(
                name: "FeatureId",
                table: "umbracoAiTrace");

            migrationBuilder.DropColumn(
                name: "FeatureType",
                table: "umbracoAiTrace");
        }
    }
}
