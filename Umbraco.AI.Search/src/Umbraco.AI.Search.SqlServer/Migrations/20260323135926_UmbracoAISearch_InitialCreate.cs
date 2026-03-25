using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Search.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAISearch_InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAISearchVectorEntry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndexName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DocumentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    Vector = table.Column<byte[]>(type: "vector(1536)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAISearchVectorEntry", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAISearchVectorEntry_IndexName",
                table: "umbracoAISearchVectorEntry",
                column: "IndexName");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAISearchVectorEntry_IndexName_DocumentId",
                table: "umbracoAISearchVectorEntry",
                columns: new[] { "IndexName", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAISearchVectorEntry_IndexName_DocumentId_ChunkIndex",
                table: "umbracoAISearchVectorEntry",
                columns: new[] { "IndexName", "DocumentId", "ChunkIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAISearchVectorEntry");
        }
    }
}
