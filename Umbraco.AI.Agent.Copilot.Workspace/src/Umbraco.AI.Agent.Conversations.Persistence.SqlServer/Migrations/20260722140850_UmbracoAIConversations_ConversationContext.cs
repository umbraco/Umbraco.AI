using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Conversations.Persistence.SqlServer.Migrations
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
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "umbracoAIConversationsConversationResource",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceTypeId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Settings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InjectionMode = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
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
