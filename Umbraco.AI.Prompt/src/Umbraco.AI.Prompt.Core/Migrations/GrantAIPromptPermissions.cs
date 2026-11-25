using Microsoft.Extensions.Logging;

using Umbraco.AI.Core.Migrations;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.AI.Prompt.Core.Security;

namespace Umbraco.AI.Prompt.Core.Migrations;

/// <summary>
/// Migration that grants the prompt permission to every user group that has the AI section, and to the Admin group.
/// </summary>
/// <remarks>
/// Follows the same rules as the core grant step: it writes straight to the CMS permission table, skips a group
/// that already holds the verb so a verb an admin turned off is not granted again, never deletes, and gives
/// nothing to groups that get the AI section later.
/// </remarks>
public class GrantAIPromptPermissions : AsyncMigrationBase
{
    // A migration is a snapshot of one release, so this list stays as it is when later prompt verbs are added.
    private static readonly string[] PromptVerbs = [AIPromptPermissions.Manage];

    private readonly AppCaches _appCaches;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrantAIPromptPermissions"/> class.
    /// </summary>
    public GrantAIPromptPermissions(IMigrationContext context, AppCaches appCaches)
        : base(context)
    {
        _appCaches = appCaches;
    }

    /// <inheritdoc/>
    protected override Task MigrateAsync()
    {
        var inserted = AIPermissionGrant.GrantToSectionGroups(Database, PromptVerbs);

        Logger.LogInformation("Granted the Umbraco AI prompt permissions to user groups ({Count} permission row(s) added)", inserted);

        AIPermissionGrant.ClearCachedUserGroups(_appCaches);

        return Task.CompletedTask;
    }
}
