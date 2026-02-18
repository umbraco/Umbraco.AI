using System.Runtime.CompilerServices;
using System.Text.Json;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.AI.Deploy.Prompt.Artifacts;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Deploy.Prompt.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for Umbraco AI Prompts, responsible for connecting the deployment process to the IAIPromptService to retrieve and save prompts during deployment.
/// </summary>
[UdiDefinition(UmbracoAIPromptConstants.UdiEntityType.Prompt, UdiType.GuidUdi)]
public class UmbracoAIPromptServiceConnector(
    IAIPromptService promptService,
    IAIProfileService profileService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIProfileDependentEntityServiceConnectorBase<AIPromptArtifact, AIPrompt>(
        profileService,
        settingsAccessor)
{
    /// <inheritdoc />
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco AI Prompts";

    /// <inheritdoc />
    public override string UdiEntityType => UmbracoAIPromptConstants.UdiEntityType.Prompt;

    /// <inheritdoc />
    public override Task<AIPrompt?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => promptService.GetPromptAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIPrompt> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prompts = await promptService.GetPromptsAsync(cancellationToken);
        foreach (var prompt in prompts)
        {
            yield return prompt;
        }
    }

    /// <inheritdoc />
    public override string GetEntityName(AIPrompt entity)
        => entity.Name;

    /// <inheritdoc />
    public override Task<AIPromptArtifact?> GetArtifactAsync(
        GuidUdi udi,
        AIPrompt? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null) return Task.FromResult<AIPromptArtifact?>(null);

        var dependencies = new ArtifactDependencyCollection();

        // Use base class helper for optional profile dependency
        var profileUdi = AddProfileDependency(entity.ProfileId, dependencies);

        var artifact = new AIPromptArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            Instructions = entity.Instructions,
            ProfileUdi = profileUdi,
            ContextIds = entity.ContextIds.ToList(),
            Tags = entity.Tags.ToList(),
            IsActive = entity.IsActive,
            IncludeEntityContext = entity.IncludeEntityContext,
            OptionCount = entity.OptionCount,
            Scope = entity.Scope != null ? JsonSerializer.SerializeToElement(entity.Scope) : null
        };

        return Task.FromResult<AIPromptArtifact?>(artifact);
    }

    /// <inheritdoc />
    public override async Task ProcessAsync(
        ArtifactDeployState<AIPromptArtifact, AIPrompt> state,
        IDeployContext context,
        int pass,
        CancellationToken cancellationToken = default)
    {
        state.NextPass = GetNextPass(pass);

        switch (pass)
        {
            case 3:
                await Pass3Async(state, context, cancellationToken);
                break;
        }
    }

    private async Task Pass3Async(
        ArtifactDeployState<AIPromptArtifact, AIPrompt> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        // Resolve optional profile dependency
        var profileId = await ResolveProfileIdAsync(artifact.ProfileUdi, cancellationToken);

        // Deserialize Scope from JsonElement
        AIPromptScope? scope = null;
        if (artifact.Scope.HasValue)
        {
            scope = JsonSerializer.Deserialize<AIPromptScope>(artifact.Scope.Value);
        }

        // Get or create prompt entity
        var prompt = state.Entity
            ?? new AIPrompt
            {
                Id = artifact.Udi.Guid,
                Alias = artifact.Alias!,
                Name = artifact.Name,
                Instructions = artifact.Instructions,
            };

        // Update properties from artifact
        prompt.Alias = artifact.Alias!;
        prompt.Name = artifact.Name;
        prompt.Description = artifact.Description;
        prompt.ProfileId = profileId;
        prompt.Instructions = artifact.Instructions;
        prompt.ContextIds = artifact.ContextIds.ToList();
        prompt.Tags = artifact.Tags.ToList();
        prompt.IsActive = artifact.IsActive;
        prompt.IncludeEntityContext = artifact.IncludeEntityContext;
        prompt.OptionCount = artifact.OptionCount;
        prompt.Scope = scope;

        state.Entity = await promptService.SavePromptAsync(prompt, cancellationToken);
    }
}
