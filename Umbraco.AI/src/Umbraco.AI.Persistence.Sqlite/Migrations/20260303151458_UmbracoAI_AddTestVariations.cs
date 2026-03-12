using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_AddTestVariations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExecutionId",
                table: "umbracoAITestRun",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "VariationId",
                table: "umbracoAITestRun",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariationName",
                table: "umbracoAITestRun",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextIds",
                table: "umbracoAITest",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "umbracoAITest",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariationsJson",
                table: "umbracoAITest",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAITestRun_ExecutionId",
                table: "umbracoAITestRun",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAITestRun_VariationId",
                table: "umbracoAITestRun",
                column: "VariationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_umbracoAITestRun_ExecutionId",
                table: "umbracoAITestRun");

            migrationBuilder.DropIndex(
                name: "IX_umbracoAITestRun_VariationId",
                table: "umbracoAITestRun");

            migrationBuilder.DropColumn(
                name: "ExecutionId",
                table: "umbracoAITestRun");

            migrationBuilder.DropColumn(
                name: "VariationId",
                table: "umbracoAITestRun");

            migrationBuilder.DropColumn(
                name: "VariationName",
                table: "umbracoAITestRun");

            migrationBuilder.DropColumn(
                name: "ContextIds",
                table: "umbracoAITest");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "umbracoAITest");

            migrationBuilder.DropColumn(
                name: "VariationsJson",
                table: "umbracoAITest");
        }
    }
}
