using Umbraco.Cms.Core.Packaging;

namespace Umbraco.AI.Prompt.Core.Migrations;

/// <summary>
/// Migration plan for the Umbraco.AI.Prompt package, for the steps that change CMS data rather than the prompt tables.
/// </summary>
public class UmbracoAIPromptMigrationPlan : PackageMigrationPlan
{
    private const string InitialStateId = "{uai-prompt-init-state}";

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIPromptMigrationPlan"/> class.
    /// </summary>
    public UmbracoAIPromptMigrationPlan()
        : base("Umbraco.AI.Prompt", "Umbraco.AI.Prompt", "UmbracoAIPrompt")
    { }

    /// <inheritdoc/>
    public override string InitialState => InitialStateId;

    /// <inheritdoc/>
    public override bool IgnoreCurrentState => false;

    /// <inheritdoc/>
    protected override void DefinePlan()
    {
        From(InitialStateId)
            .To<GrantAIPromptPermissions>("49282B99-1F53-4CE1-9DDA-71C832E52577");
    }
}
