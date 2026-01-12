using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Ai.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAi_AddGovernanceTracing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAiTrace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SpanId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorCategory = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileAlias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    TotalTokens = table.Column<int>(type: "int", nullable: true),
                    PromptSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetailLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiTrace", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAiExecutionSpan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpanId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ParentSpanId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    SpanName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SpanType = table.Column<int>(type: "int", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InputData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: true),
                    TokensUsed = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAiExecutionSpan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAiExecutionSpan_umbracoAiTrace_TraceId",
                        column: x => x.TraceId,
                        principalTable: "umbracoAiTrace",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiExecutionSpan_SpanId",
                table: "umbracoAiExecutionSpan",
                column: "SpanId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiExecutionSpan_TraceId",
                table: "umbracoAiExecutionSpan",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiExecutionSpan_TraceId_SequenceNumber",
                table: "umbracoAiExecutionSpan",
                columns: new[] { "TraceId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTrace_EntityId_EntityType",
                table: "umbracoAiTrace",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTrace_ProfileId",
                table: "umbracoAiTrace",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTrace_StartTime",
                table: "umbracoAiTrace",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTrace_StartTime_Status",
                table: "umbracoAiTrace",
                columns: new[] { "StartTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTrace_Status",
                table: "umbracoAiTrace",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTrace_TraceId",
                table: "umbracoAiTrace",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAiTrace_UserId",
                table: "umbracoAiTrace",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAiExecutionSpan");

            migrationBuilder.DropTable(
                name: "umbracoAiTrace");
        }
    }
}
