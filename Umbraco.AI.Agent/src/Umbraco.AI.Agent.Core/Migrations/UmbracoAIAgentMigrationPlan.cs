using Umbraco.Cms.Core.Packaging;

namespace Umbraco.AI.Agent.Core.Migrations;

/// <summary>
/// Migration plan for the Umbraco.AI.Agent package, for the steps that change CMS data rather than the agent tables.
/// </summary>
public class UmbracoAIAgentMigrationPlan : PackageMigrationPlan
{
    private const string InitialStateId = "{uai-agent-init-state}";

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIAgentMigrationPlan"/> class.
    /// </summary>
    public UmbracoAIAgentMigrationPlan()
        : base("Umbraco.AI.Agent", "Umbraco.AI.Agent", "UmbracoAIAgent")
    { }

    /// <inheritdoc/>
    public override string InitialState => InitialStateId;

    /// <inheritdoc/>
    public override bool IgnoreCurrentState => false;

    /// <inheritdoc/>
    protected override void DefinePlan()
    {
        From(InitialStateId)
            .To<GrantAIAgentPermissions>("76A5DD78-68FB-4B32-89BA-346C1EE12A25");
    }
}
