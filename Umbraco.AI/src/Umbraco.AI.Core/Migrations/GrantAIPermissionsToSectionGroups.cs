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
        var inserted = AIPermissionGrant.GrantToSectionGroups(Database, CoreVerbs);

        Logger.LogInformation("Granted the core Umbraco AI permissions to user groups ({Count} permission row(s) added)", inserted);

        AIPermissionGrant.ClearCachedUserGroups(_appCaches);

        return Task.CompletedTask;
    }
}
