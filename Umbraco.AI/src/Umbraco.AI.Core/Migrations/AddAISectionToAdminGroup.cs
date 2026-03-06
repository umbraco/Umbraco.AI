using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Umbraco.AI.Core.Migrations;

/// <summary>
/// Migration to add the AI section to the Admin user group.
/// </summary>
/// <remarks>
/// Uses direct database access instead of <c>IUserGroupService</c> because the service layer
/// performs authorization checks and publishes notifications that may fail during migration context.
/// </remarks>
public class AddAISectionToAdminGroup : AsyncMigrationBase
{
    private const string UserGroup2AppTable = Cms.Core.Constants.DatabaseSchema.Tables.UserGroup2App;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAISectionToAdminGroup"/> class.
    /// </summary>
    public AddAISectionToAdminGroup(IMigrationContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    protected override Task MigrateAsync()
    {
        var quotedTable = SqlSyntax.GetQuotedTableName(UserGroup2AppTable);

        // Check if the AI section is already assigned to the admin group (userGroupId = 1)
        var exists = Database.ExecuteScalar<int>(
            Sql($"SELECT COUNT(*) FROM {quotedTable} WHERE userGroupId = @0 AND app = @1", 1, Constants.Sections.AI));

        if (exists > 0)
        {
            Logger.LogDebug("The Umbraco AI Application/Section has been assigned to the Admin group already");
            return Task.CompletedTask;
        }

        Database.Execute(
            Sql($"INSERT INTO {quotedTable} (userGroupId, app) VALUES (@0, @1)", 1, Constants.Sections.AI));

        Logger.LogInformation("The Umbraco AI Application/Section has been assigned to the Admin group");

        return Task.CompletedTask;
    }
}
