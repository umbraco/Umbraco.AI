using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Conversations.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIConversations_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAIConversationsProject",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContextIds = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIConversationsProject", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAIConversationsConversation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentIdOrAlias = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIConversationsConversation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAIConversationsConversation_umbracoAIConversationsProject_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "umbracoAIConversationsProject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAIConversationsProjectResource",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceTypeId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Settings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InjectionMode = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIConversationsProjectResource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAIConversationsProjectResource_umbracoAIConversationsProject_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "umbracoAIConversationsProject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAIConversationsMessage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIConversationsMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAIConversationsMessage_umbracoAIConversationsConversation_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "umbracoAIConversationsConversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIConversationsConversation_LastMessageAt",
                table: "umbracoAIConversationsConversation",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIConversationsConversation_ProjectId",
                table: "umbracoAIConversationsConversation",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIConversationsConversation_UserKey",
                table: "umbracoAIConversationsConversation",
                column: "UserKey");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIConversationsMessage_ConversationId_Sequence",
                table: "umbracoAIConversationsMessage",
                columns: new[] { "ConversationId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIConversationsProject_UserKey",
                table: "umbracoAIConversationsProject",
                column: "UserKey");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIConversationsProjectResource_ProjectId",
                table: "umbracoAIConversationsProjectResource",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAIConversationsMessage");

            migrationBuilder.DropTable(
                name: "umbracoAIConversationsProjectResource");

            migrationBuilder.DropTable(
                name: "umbracoAIConversationsConversation");

            migrationBuilder.DropTable(
                name: "umbracoAIConversationsProject");
        }
    }
}
