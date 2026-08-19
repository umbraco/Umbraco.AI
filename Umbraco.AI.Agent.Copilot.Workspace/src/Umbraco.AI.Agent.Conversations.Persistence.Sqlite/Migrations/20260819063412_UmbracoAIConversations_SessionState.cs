using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Conversations.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIConversations_SessionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionStateJson",
                table: "umbracoAIConversationsConversation",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionStateJson",
                table: "umbracoAIConversationsConversation");
        }
    }
}
