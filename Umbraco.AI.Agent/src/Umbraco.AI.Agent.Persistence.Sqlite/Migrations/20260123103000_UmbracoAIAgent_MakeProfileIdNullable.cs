using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.AI.Agent.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoAIAgent_MakeProfileIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite doesn't support AlterColumn for changing nullability
            // We need to rebuild the table with the correct schema

            // 1. Rename the existing table
            migrationBuilder.RenameTable(
                name: "umbracoAIAgent",
                newName: "umbracoAIAgent_old");

            // 2. Create the new table with ProfileId as nullable
            migrationBuilder.CreateTable(
                name: "umbracoAIAgent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Instructions = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    ContextIds = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIAgent", x => x.Id);
                });

            // 3. Copy data from old table to new table
            migrationBuilder.Sql(@"
                INSERT INTO umbracoAIAgent
                (Id, Alias, Name, Description, ProfileId, Instructions, IsActive, ContextIds, Version, DateCreated, DateModified, CreatedByUserId, ModifiedByUserId)
                SELECT Id, Alias, Name, Description, ProfileId, Instructions, IsActive, ContextIds, Version, DateCreated, DateModified, CreatedByUserId, ModifiedByUserId
                FROM umbracoAIAgent_old;
            ");

            // 4. Drop the old table
            migrationBuilder.DropTable(name: "umbracoAIAgent_old");

            // 5. Recreate indexes
            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAgent_Alias",
                table: "umbracoAIAgent",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAgent_ProfileId",
                table: "umbracoAIAgent",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse operation: make ProfileId NOT NULL again
            migrationBuilder.RenameTable(
                name: "umbracoAIAgent",
                newName: "umbracoAIAgent_old");

            migrationBuilder.CreateTable(
                name: "umbracoAIAgent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Instructions = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    ContextIds = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoAIAgent", x => x.Id);
                });

            migrationBuilder.Sql(@"
                INSERT INTO umbracoAIAgent
                (Id, Alias, Name, Description, ProfileId, Instructions, IsActive, ContextIds, Version, DateCreated, DateModified, CreatedByUserId, ModifiedByUserId)
                SELECT Id, Alias, Name, Description, ProfileId, Instructions, IsActive, ContextIds, Version, DateCreated, DateModified, CreatedByUserId, ModifiedByUserId
                FROM umbracoAIAgent_old
                WHERE ProfileId IS NOT NULL;
            ");

            migrationBuilder.DropTable(name: "umbracoAIAgent_old");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAgent_Alias",
                table: "umbracoAIAgent",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoAIAgent_ProfileId",
                table: "umbracoAIAgent",
                column: "ProfileId");
        }
    }
}
