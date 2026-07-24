using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Conversations.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIConversations_ConversationContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContextIds",
                table: "umbracoAIConversationsConversation",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "umbracoAIConversationsConversationResource",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConversationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ResourceTypeId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Settings = table.Column<string>(type: "TEXT", nullable: true),
                    InjectionMode = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIConversationsConversationResource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAIConversationsConversationResource_umbracoAIConversationsConversation_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "umbracoAIConversationsConversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIConversationsConversationResource_ConversationId",
                table: "umbracoAIConversationsConversationResource",
                column: "ConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAIConversationsConversationResource");

            migrationBuilder.DropColumn(
                name: "ContextIds",
                table: "umbracoAIConversationsConversation");
        }
    }
}
