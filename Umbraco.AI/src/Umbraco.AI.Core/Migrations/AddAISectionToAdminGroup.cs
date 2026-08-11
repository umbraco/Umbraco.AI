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
    private const string UserGroupTable = Cms.Core.Constants.DatabaseSchema.Tables.UserGroup;
    private const string UserGroup2AppTable = Cms.Core.Constants.DatabaseSchema.Tables.UserGroup2App;
    private const string KeyColumn = Cms.Core.Constants.DatabaseSchema.Columns.PrimaryKeyNameKey;

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
        var quotedUserGroupTable = SqlSyntax.GetQuotedTableName(UserGroupTable);
        var quotedKeyColumn = SqlSyntax.GetQuotedColumnName(KeyColumn);
        var quotedTable = SqlSyntax.GetQuotedTableName(UserGroup2AppTable);

        // Resolve the admin group by its well-known key rather than assuming id = 1, since the
        // seeded admin group's id can differ on sites where user groups have been recreated.
        var adminGroupId = Database.ExecuteScalar<int?>(
            Sql($"SELECT id FROM {quotedUserGroupTable} WHERE {quotedKeyColumn} = @0", Cms.Core.Constants.Security.AdminGroupKey));

        if (adminGroupId is null)
        {
            Logger.LogWarning("Could not find the Admin user group; skipping assignment of the Umbraco AI Application/Section");
            return Task.CompletedTask;
        }

        var exists = Database.ExecuteScalar<int>(
            Sql($"SELECT COUNT(*) FROM {quotedTable} WHERE userGroupId = @0 AND app = @1", adminGroupId, Constants.Sections.AI));

        if (exists > 0)
        {
            Logger.LogDebug("The Umbraco AI Application/Section has been assigned to the Admin group already");
            return Task.CompletedTask;
        }

        Database.Execute(
            Sql($"INSERT INTO {quotedTable} (userGroupId, app) VALUES (@0, @1)", adminGroupId, Constants.Sections.AI));

        Logger.LogInformation("The Umbraco AI Application/Section has been assigned to the Admin group");

        return Task.CompletedTask;
    }
}
