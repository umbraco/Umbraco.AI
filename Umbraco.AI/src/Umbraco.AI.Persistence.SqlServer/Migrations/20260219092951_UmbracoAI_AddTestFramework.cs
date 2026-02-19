using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_AddTestFramework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAITest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TestTypeId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetIsAlias = table.Column<bool>(type: "bit", nullable: false),
                    TestCaseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GradersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RunCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Tags = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BaselineRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAITest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAITestRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestVersion = table.Column<int>(type: "int", nullable: false),
                    RunNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContextIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    TranscriptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutcomeType = table.Column<int>(type: "int", nullable: false),
                    OutcomeValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutcomeFinishReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OutcomeTokenUsageJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GraderResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAITestRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAITestTranscript",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessagesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToolCallsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasoningJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalOutputJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAITestTranscript", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAITest_Alias",
                table: "umbracoAITest",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAITest_IsActive",
                table: "umbracoAITest",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAITest_TestTypeId",
                table: "umbracoAITest",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAITestRun_BatchId",
                table: "umbracoAITestRun",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAITestRun_ExecutedAt",
                table: "umbracoAITestRun",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAITestRun_TestId",
                table: "umbracoAITestRun",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAITestRun_TestId_ExecutedAt",
                table: "umbracoAITestRun",
                columns: new[] { "TestId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAITestTranscript_RunId",
                table: "umbracoAITestTranscript",
                column: "RunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAITest");

            migrationBuilder.DropTable(
                name: "umbracoAITestRun");

            migrationBuilder.DropTable(
                name: "umbracoAITestTranscript");
        }
    }
}
