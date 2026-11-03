using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Conversations.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIConversations_AddShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAIConversationsShare",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedByUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeMode = table.Column<int>(type: "int", nullable: false),
                    CutoffMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateShared = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateRevoked = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIConversationsShare", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAIConversationsShare_umbracoAIConversationsConversation_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "umbracoAIConversationsConversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIConversationsShare_ConversationId_SharedWithUserKey",
                table: "umbracoAIConversationsShare",
                columns: new[] { "ConversationId", "SharedWithUserKey" },
                unique: true,
                filter: "DateRevoked IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIConversationsShare_SharedWithUserKey",
                table: "umbracoAIConversationsShare",
                column: "SharedWithUserKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAIConversationsShare");
        }
    }
}
