using Microsoft.Extensions.Logging;

using Umbraco.AI.Core.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Umbraco.AI.Core.Migrations;

/// <summary>
/// Migration that grants the core AI permissions to every user group that has the AI section, and to the Admin group.
/// </summary>
/// <remarks>
/// <para>
/// Writes straight to the CMS fallback permission table (<c>umbracoUserGroup2Permission</c>), the same rows the
/// user group screen saves, for the same reason as <see cref="AddAISectionToAdminGroup"/>: the user group service
/// performs authorization checks and publishes notifications that may fail in a migration context.
/// </para>
/// <para>
/// A group that already holds any of the core verbs is treated as already upgraded and left alone, so a verb an
/// admin turned off is not granted again if this step ever runs a second time, and no row is added twice.
/// The migration never deletes. Groups given the AI section after the upgrade get no rights until an admin ticks them.
/// A group with all nine core verbs turned off looks the same as a group never upgraded, so it would get them
/// back if this step ever re-runs, which only happens if its stored migration state is reset.
/// </para>
/// </remarks>
public class GrantAIPermissionsToSectionGroups : AsyncMigrationBase
{
    private const string UserGroupTable = Cms.Core.Constants.DatabaseSchema.Tables.UserGroup;
    private const string UserGroup2AppTable = Cms.Core.Constants.DatabaseSchema.Tables.UserGroup2App;
    private const string UserGroup2PermissionTable = Cms.Core.Constants.DatabaseSchema.Tables.UserGroup2Permission;
    private const string KeyColumn = Cms.Core.Constants.DatabaseSchema.Columns.PrimaryKeyNameKey;

    private readonly AppCaches _appCaches;

    // A migration is a snapshot of one release. This list stays as it is when later verbs are added to
    // AIPermissions: a later verb gets its own migration, so it is not granted to groups that never had it.
    // Agent and Prompt verbs are granted by their own products' migrations.
    private static readonly string[] CoreVerbs =
    [
        AIPermissions.ManageConnections,
        AIPermissions.ManageProfiles,
        AIPermissions.ManageContexts,
        AIPermissions.ManageKnowledgeSets,
        AIPermissions.ManageGuardrails,
        AIPermissions.ManageSettings,
        AIPermissions.ViewAuditLogs,
        AIPermissions.ViewAnalytics,
        AIPermissions.ManageTests,
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="GrantAIPermissionsToSectionGroups"/> class.
    /// </summary>
    public GrantAIPermissionsToSectionGroups(IMigrationContext context, AppCaches appCaches)
        : base(context)
    {
        _appCaches = appCaches;
    }

    /// <inheritdoc/>
    protected override Task MigrateAsync()
    {
        var quotedGroupTable = SqlSyntax.GetQuotedTableName(UserGroupTable);
        var quotedAppTable = SqlSyntax.GetQuotedTableName(UserGroup2AppTable);
        var quotedPermissionTable = SqlSyntax.GetQuotedTableName(UserGroup2PermissionTable);
        var quotedKey = SqlSyntax.GetQuotedColumnName(KeyColumn);

        // Parameters: @0 admin group key, @1 AI section alias, @2..@10 the verbs.
        var verbParameters = Enumerable.Range(2, CoreVerbs.Length).Select(i => $"@{i}").ToArray();
        var verbRows = string.Join(" UNION ALL ", verbParameters.Select(p => $"SELECT {p} AS permission"));
        var verbList = string.Join(", ", verbParameters);

        object[] parameters = [Cms.Core.Constants.Security.AdminGroupKey, Constants.Sections.AI, .. CoreVerbs];

        var inserted = Database.Execute(
            Sql(
                $"""
                INSERT INTO {quotedPermissionTable} (userGroupKey, permission)
                SELECT g.{quotedKey}, v.permission
                FROM {quotedGroupTable} g
                CROSS JOIN ({verbRows}) v
                WHERE (g.{quotedKey} = @0 OR g.id IN (SELECT userGroupId FROM {quotedAppTable} WHERE app = @1))
                AND NOT EXISTS (
                    SELECT 1 FROM {quotedPermissionTable} p
                    WHERE p.userGroupKey = g.{quotedKey} AND p.permission IN ({verbList}))
                """,
                parameters));

        Logger.LogInformation("Granted the core Umbraco AI permissions to user groups ({Count} permission row(s) added)", inserted);

        // Clear only the cached user groups, and the cached users that carry a copy of their groups, so the new
        // rights show without a restart. RebuildCache is avoided as it also rebuilds the published content cache.
        _appCaches.IsolatedCaches.ClearCache<IUserGroup>();
        _appCaches.IsolatedCaches.ClearCache<IUser>();

        return Task.CompletedTask;
    }
}
