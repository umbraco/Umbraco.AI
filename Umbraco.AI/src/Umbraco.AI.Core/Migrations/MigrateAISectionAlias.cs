using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Umbraco.AI.Core.Migrations;

/// <summary>
/// Migration to update the AI section alias from "Uai.Section.AI" to "ai" in user group assignments.
/// </summary>
/// <remarks>
/// The old alias "Uai.Section.AI" doesn't match Umbraco's convention of simple lowercase section aliases
/// (e.g., "content", "media", "settings") which are used for endpoint permission checks.
/// </remarks>
public class MigrateAISectionAlias : AsyncMigrationBase
{
    private const string UserGroup2AppTable = Cms.Core.Constants.DatabaseSchema.Tables.UserGroup2App;
    private const string OldAlias = "Uai.Section.AI";

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateAISectionAlias"/> class.
    /// </summary>
    public MigrateAISectionAlias(IMigrationContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    protected override Task MigrateAsync()
    {
        var quotedTable = SqlSyntax.GetQuotedTableName(UserGroup2AppTable);

        // Delete old alias rows where the user group already has the new alias (avoid PK conflict)
        Database.Execute(
            Sql($"DELETE FROM {quotedTable} WHERE app = @0 AND userGroupId IN (SELECT userGroupId FROM {quotedTable} WHERE app = @1)",
                OldAlias, Constants.Sections.AI));

        // Update any remaining old alias rows to the new alias
        var updated = Database.Execute(
            Sql($"UPDATE {quotedTable} SET app = @0 WHERE app = @1", Constants.Sections.AI, OldAlias));

        if (updated > 0)
        {
            Logger.LogInformation("Migrated {Count} user group AI section assignment(s) from '{OldAlias}' to '{NewAlias}'",
                updated, OldAlias, Constants.Sections.AI);
        }

        return Task.CompletedTask;
    }
}
