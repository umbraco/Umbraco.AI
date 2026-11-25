using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Infrastructure.Persistence;

namespace Umbraco.AI.Core.Migrations;

/// <summary>
/// The shared steps of the migrations that grant a product's AI permission verbs to existing user groups.
/// </summary>
/// <remarks>
/// Each product keeps its own migration and its own verb list, because a migration is a snapshot of one release.
/// Only the SQL and the cache clearing live here, so they are written once.
/// </remarks>
internal static class AIPermissionGrant
{
    private const string UserGroupTable = Cms.Core.Constants.DatabaseSchema.Tables.UserGroup;
    private const string UserGroup2AppTable = Cms.Core.Constants.DatabaseSchema.Tables.UserGroup2App;
    private const string UserGroup2PermissionTable = Cms.Core.Constants.DatabaseSchema.Tables.UserGroup2Permission;
    private const string KeyColumn = Cms.Core.Constants.DatabaseSchema.Columns.PrimaryKeyNameKey;

    /// <summary>
    /// Grants the verbs to every user group with the AI section and to the Admin group, skipping a group that
    /// already holds any of the verbs. Returns the number of permission rows added.
    /// </summary>
    internal static int GrantToSectionGroups(IUmbracoDatabase database, IReadOnlyList<string> verbs)
    {
        var syntax = database.SqlContext.SqlSyntax;
        var quotedGroupTable = syntax.GetQuotedTableName(UserGroupTable);
        var quotedAppTable = syntax.GetQuotedTableName(UserGroup2AppTable);
        var quotedPermissionTable = syntax.GetQuotedTableName(UserGroup2PermissionTable);
        var quotedKey = syntax.GetQuotedColumnName(KeyColumn);

        // Parameters: @0 admin group key, @1 AI section alias, @2.. the verbs.
        var verbParameters = Enumerable.Range(2, verbs.Count).Select(i => $"@{i}").ToArray();
        var verbRows = string.Join(" UNION ALL ", verbParameters.Select(p => $"SELECT {p} AS permission"));
        var verbList = string.Join(", ", verbParameters);

        object[] parameters = [Cms.Core.Constants.Security.AdminGroupKey, Constants.Sections.AI, .. verbs];

        return database.Execute(
            database.SqlContext.Sql(
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
    }

    /// <summary>
    /// Clears only the cached user groups, and the cached users that carry a copy of their groups, so the new
    /// rights show without a restart. RebuildCache is avoided as it also rebuilds the published content cache.
    /// </summary>
    internal static void ClearCachedUserGroups(AppCaches appCaches)
    {
        appCaches.IsolatedCaches.ClearCache<IUserGroup>();
        appCaches.IsolatedCaches.ClearCache<IUser>();
    }
}
