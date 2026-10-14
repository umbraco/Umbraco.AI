using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_AddEntityVersionAuthor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AuthoredByUserId",
                table: "umbracoAIEntityVersion",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateAuthored",
                table: "umbracoAIEntityVersion",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Existing rows never recorded the author separately; their created values are the best available.
            migrationBuilder.Sql(
                "UPDATE umbracoAIEntityVersion SET DateAuthored = DateCreated, AuthoredByUserId = CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthoredByUserId",
                table: "umbracoAIEntityVersion");

            migrationBuilder.DropColumn(
                name: "DateAuthored",
                table: "umbracoAIEntityVersion");
        }
    }
}
