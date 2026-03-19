using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_AddEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAIEmbedding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntitySubType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TextContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Vector = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Dimensions = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DateIndexed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntityDateModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIEmbedding", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIEmbedding_EntityKey",
                table: "umbracoAIEmbedding",
                column: "EntityKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIEmbedding_EntityType",
                table: "umbracoAIEmbedding",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIEmbedding_ProfileId",
                table: "umbracoAIEmbedding",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAIEmbedding");
        }
    }
}
