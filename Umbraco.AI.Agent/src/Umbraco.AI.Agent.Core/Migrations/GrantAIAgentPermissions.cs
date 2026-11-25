using Microsoft.Extensions.Logging;

using Umbraco.AI.Agent.Core.Security;
using Umbraco.AI.Core.Migrations;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Umbraco.AI.Agent.Core.Migrations;

/// <summary>
/// Migration that grants the agent permission to every user group that has the AI section, and to the Admin group.
/// </summary>
/// <remarks>
/// Follows the same rules as the core grant step: it writes straight to the CMS permission table, skips a group
/// that already holds the verb so a verb an admin turned off is not granted again, never deletes, and gives
/// nothing to groups that get the AI section later.
/// </remarks>
public class GrantAIAgentPermissions : AsyncMigrationBase
{
    // A migration is a snapshot of one release, so this list stays as it is when later agent verbs are added.
    private static readonly string[] AgentVerbs = [AIAgentPermissions.Manage];

    private readonly AppCaches _appCaches;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrantAIAgentPermissions"/> class.
    /// </summary>
    public GrantAIAgentPermissions(IMigrationContext context, AppCaches appCaches)
        : base(context)
    {
        _appCaches = appCaches;
    }

    /// <inheritdoc/>
    protected override Task MigrateAsync()
    {
        var inserted = AIPermissionGrant.GrantToSectionGroups(Database, AgentVerbs);

        Logger.LogInformation("Granted the Umbraco AI agent permissions to user groups ({Count} permission row(s) added)", inserted);

        AIPermissionGrant.ClearCachedUserGroups(_appCaches);

        return Task.CompletedTask;
    }
}
