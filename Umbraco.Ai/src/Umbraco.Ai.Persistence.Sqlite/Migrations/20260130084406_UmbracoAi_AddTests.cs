using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAiTest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TestTypeId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    TargetIsAlias = table.Column<bool>(type: "INTEGER", nullable: false),
                    TestCaseJson = table.Column<string>(type: "TEXT", nullable: false),
                    RunCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    BaselineRunId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiTest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAiTestGrader",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraderTypeId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ConfigJson = table.Column<string>(type: "TEXT", nullable: false),
                    Negate = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 2),
                    Weight = table.Column<float>(type: "REAL", nullable: false, defaultValue: 1f),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiTestGrader", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiTestGrader_umbracoAiTest_TestId",
                        column: x => x.TestId,
                        principalTable: "umbracoAiTest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAiTestRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    RunNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContextIdsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExecutedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    OutcomeType = table.Column<int>(type: "INTEGER", nullable: false),
                    OutcomeValue = table.Column<string>(type: "TEXT", nullable: false),
                    FinishReason = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    InputTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    BatchId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiTestRun", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiTestRun_umbracoAiTest_TestId",
                        column: x => x.TestId,
                        principalTable: "umbracoAiTest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAiTestGraderResult",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Passed = table.Column<bool>(type: "INTEGER", nullable: false),
                    Score = table.Column<float>(type: "REAL", nullable: true),
                    ActualValue = table.Column<string>(type: "TEXT", nullable: true),
                    ExpectedValue = table.Column<string>(type: "TEXT", nullable: true),
                    FailureMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiTestGraderResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiTestGraderResult_umbracoAiTestGrader_GraderId",
                        column: x => x.GraderId,
                        principalTable: "umbracoAiTestGrader",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_umbracoAiTestGraderResult_umbracoAiTestRun_RunId",
                        column: x => x.RunId,
                        principalTable: "umbracoAiTestRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAiTestTranscript",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessagesJson = table.Column<string>(type: "TEXT", nullable: false),
                    ToolCallsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ReasoningJson = table.Column<string>(type: "TEXT", nullable: true),
                    TimingJson = table.Column<string>(type: "TEXT", nullable: true),
                    FinalOutputJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiTestTranscript", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiTestTranscript_umbracoAiTestRun_RunId",
                        column: x => x.RunId,
                        principalTable: "umbracoAiTestRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTest_Alias",
                table: "umbracoAiTest",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTest_IsEnabled",
                table: "umbracoAiTest",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTest_Tags",
                table: "umbracoAiTest",
                column: "Tags");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTest_TestTypeId",
                table: "umbracoAiTest",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTestGrader_GraderTypeId",
                table: "umbracoAiTestGrader",
                column: "GraderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTestGrader_TestId",
                table: "umbracoAiTestGrader",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTestGraderResult_GraderId",
                table: "umbracoAiTestGraderResult",
                column: "GraderId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTestGraderResult_Passed",
                table: "umbracoAiTestGraderResult",
                column: "Passed");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTestGraderResult_RunId",
                table: "umbracoAiTestGraderResult",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTestRun_BatchId",
                table: "umbracoAiTestRun",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTestRun_ExecutedAt",
                table: "umbracoAiTestRun",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTestRun_ProfileId",
                table: "umbracoAiTestRun",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTestRun_Status",
                table: "umbracoAiTestRun",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTestRun_TestId_RunNumber",
                table: "umbracoAiTestRun",
                columns: new[] { "TestId", "RunNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTestTranscript_RunId",
                table: "umbracoAiTestTranscript",
                column: "RunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiTestGraderResult");

            migrationBuilder.DropTable(
                name: "umbracoAiTestTranscript");

            migrationBuilder.DropTable(
                name: "umbracoAiTestGrader");

            migrationBuilder.DropTable(
                name: "umbracoAiTestRun");

            migrationBuilder.DropTable(
                name: "umbracoAiTest");
        }
    }
}
