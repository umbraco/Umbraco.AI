using Umbraco.Cms.Core.Packaging;

namespace Umbraco.AI.Agent.Copilot.Workspace.Core.Migrations;

/// <summary>
/// Package migration plan for the Copilot Workspace product. Auto-discovered and run by Umbraco on
/// startup; mirrors <c>UmbracoAIMigrationPlan</c>. (The conversation/message/project schema is managed
/// separately via EF Core migrations; this plan covers CMS-level setup such as the section grant.)
/// </summary>
public class CopilotWorkspaceMigrationPlan : PackageMigrationPlan
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotWorkspaceMigrationPlan"/> class.
    /// </summary>
    public CopilotWorkspaceMigrationPlan()
        : base("Umbraco.AI.Agent.Copilot.Workspace", "Umbraco.AI.Agent.Copilot.Workspace", "UmbracoAICopilotWorkspace")
    {
    }

    /// <inheritdoc/>
    public override string InitialState => "{uai-copilot-workspace-init-state}";

    /// <inheritdoc/>
    public override bool IgnoreCurrentState => false;

    /// <inheritdoc/>
    protected override void DefinePlan()
    {
        From("{uai-copilot-workspace-init-state}")
            .To<AddCopilotWorkspaceSectionToAdminGroup>("6F2A9C41-7D3E-4B58-9E1A-0C7B4D2F8A63");
    }
}
