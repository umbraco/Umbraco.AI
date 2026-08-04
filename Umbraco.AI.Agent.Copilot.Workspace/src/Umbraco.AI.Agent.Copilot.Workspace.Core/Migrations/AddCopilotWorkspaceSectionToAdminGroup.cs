using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Umbraco.AI.Agent.Copilot.Workspace.Core.Migrations;

/// <summary>
/// Migration that adds the Copilot Workspace section to the Admin user group, so administrators can
/// reach the Workspace out of the box (mirrors Umbraco.AI's <c>AddAISectionToAdminGroup</c>).
/// </summary>
/// <remarks>
/// Uses direct database access instead of <c>IUserGroupService</c> because the service layer performs
/// authorization checks and publishes notifications that may fail during migration context.
/// </remarks>
public class AddCopilotWorkspaceSectionToAdminGroup : AsyncMigrationBase
{
    private const string UserGroup2AppTable = Cms.Core.Constants.DatabaseSchema.Tables.UserGroup2App;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddCopilotWorkspaceSectionToAdminGroup"/> class.
    /// </summary>
    public AddCopilotWorkspaceSectionToAdminGroup(IMigrationContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    protected override Task MigrateAsync()
    {
        var quotedTable = SqlSyntax.GetQuotedTableName(UserGroup2AppTable);
        var section = CopilotWorkspaceConstants.Sections.CopilotWorkspace;

        // Check if the section is already assigned to the admin group (userGroupId = 1).
        var exists = Database.ExecuteScalar<int>(
            Sql($"SELECT COUNT(*) FROM {quotedTable} WHERE userGroupId = @0 AND app = @1", 1, section));

        if (exists > 0)
        {
            Logger.LogDebug("The Copilot Workspace section has been assigned to the Admin group already");
            return Task.CompletedTask;
        }

        Database.Execute(
            Sql($"INSERT INTO {quotedTable} (userGroupId, app) VALUES (@0, @1)", 1, section));

        Logger.LogInformation("The Copilot Workspace section has been assigned to the Admin group");

        return Task.CompletedTask;
    }
}
