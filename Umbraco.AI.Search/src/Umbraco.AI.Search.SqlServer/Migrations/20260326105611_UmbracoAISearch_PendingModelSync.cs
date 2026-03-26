using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Search.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAISearch_PendingModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_umbracoAISearchVectorEntry_IndexName_DocumentId_Culture_ChunkIndex",
                table: "umbracoAISearchVectorEntry");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAISearchVectorEntry_IndexName_DocumentId_Culture_ChunkIndex",
                table: "umbracoAISearchVectorEntry",
                columns: new[] { "IndexName", "DocumentId", "Culture", "ChunkIndex" },
                unique: true,
                filter: "[Culture] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_umbracoAISearchVectorEntry_IndexName_DocumentId_Culture_ChunkIndex",
                table: "umbracoAISearchVectorEntry");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAISearchVectorEntry_IndexName_DocumentId_Culture_ChunkIndex",
                table: "umbracoAISearchVectorEntry",
                columns: new[] { "IndexName", "DocumentId", "Culture", "ChunkIndex" },
                unique: true);
        }
    }
}
