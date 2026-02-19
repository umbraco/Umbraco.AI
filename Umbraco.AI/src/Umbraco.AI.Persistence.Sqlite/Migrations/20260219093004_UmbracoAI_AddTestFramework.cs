using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.Sqlite.Migrations
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TestFeatureId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TargetIsAlias = table.Column<bool>(type: "INTEGER", nullable: false),
                    TestCaseJson = table.Column<string>(type: "TEXT", nullable: false),
                    GradersJson = table.Column<string>(type: "TEXT", nullable: true),
                    RunCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    BaselineRunId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAITest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAITestRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    RunNumber = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContextIds = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExecutedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    TranscriptId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OutcomeType = table.Column<int>(type: "INTEGER", nullable: false),
                    OutcomeValue = table.Column<string>(type: "TEXT", nullable: true),
                    OutcomeFinishReason = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OutcomeTokenUsageJson = table.Column<string>(type: "TEXT", nullable: true),
                    GraderResultsJson = table.Column<string>(type: "TEXT", nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    BatchId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAITestRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAITestTranscript",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessagesJson = table.Column<string>(type: "TEXT", nullable: true),
                    ToolCallsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ReasoningJson = table.Column<string>(type: "TEXT", nullable: true),
                    TimingJson = table.Column<string>(type: "TEXT", nullable: true),
                    FinalOutputJson = table.Column<string>(type: "TEXT", nullable: false)
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
                name: "IX_umbracoAITest_TestFeatureId",
                table: "umbracoAITest",
                column: "TestFeatureId");

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
