using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAI_AddGuardrails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoAIGuardrail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIGuardrail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "umbracoAIGuardrailRule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuardrailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phase = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Config = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIGuardrailRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_umbracoAIGuardrailRule_umbracoAIGuardrail_GuardrailId",
                        column: x => x.GuardrailId,
                        principalTable: "umbracoAIGuardrail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIGuardrail_Alias",
                table: "umbracoAIGuardrail",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIGuardrailRule_EvaluatorId",
                table: "umbracoAIGuardrailRule",
                column: "EvaluatorId");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIGuardrailRule_GuardrailId",
                table: "umbracoAIGuardrailRule",
                column: "GuardrailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoAIGuardrailRule");

            migrationBuilder.DropTable(
                name: "umbracoAIGuardrail");
        }
    }
}
