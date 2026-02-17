using System.Text.Json;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.AI.Deploy.Extensions;
using Umbraco.AI.Deploy.Prompt.Artifacts;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Core;

namespace Umbraco.AI.Deploy.Prompt.Connectors.ServiceConnectors;

[UdiDefinition(UmbracoAIPromptConstants.UdiEntityType.Prompt, UdiType.GuidUdi)]
public class UmbracoAIPromptServiceConnector(
    IAIPromptService promptService,
    IAIProfileService profileService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIProfileDependentEntityServiceConnectorBase<AIPromptArtifact, AIPrompt>(
        profileService,
        settingsAccessor)
{
    private readonly IAIPromptService _promptService = promptService;

    public override string UdiEntityType => UmbracoAIPromptConstants.UdiEntityType.Prompt;

    public override Task<AIPrompt?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => _promptService.GetPromptAsync(id, cancellationToken);

    public override async IAsyncEnumerable<AIPrompt> GetEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var prompts = await _promptService.GetPromptsAsync(cancellationToken);
        foreach (var prompt in prompts)
        {
            yield return prompt;
        }
    }

    public override string GetEntityName(AIPrompt entity)
        => entity.Name;

    public override Task<AIPromptArtifact?> GetArtifactAsync(
        GuidUdi? udi,
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
            Scope = entity.Scope != null ? JsonSerializer.SerializeToElement(entity.Scope) : null,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId,
            Version = entity.Version
        };

        return Task.FromResult<AIPromptArtifact?>(artifact);
    }

    public override async Task ProcessAsync(
        ArtifactDeployState<AIPromptArtifact, AIPrompt> state,
        IDeployContext context,
        int pass,
        CancellationToken cancellationToken = default)
    {
        state.NextPass = GetNextPass(pass);

        switch (pass)
        {
            case 2:
                await Pass2Async(state, context, cancellationToken);
                break;
            case 4:
                await Pass4Async(state, context, cancellationToken);
                break;
        }
    }

    private async Task Pass2Async(
        ArtifactDeployState<AIPromptArtifact, AIPrompt> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        // Deserialize Scope from JsonElement
        AIPromptScope? scope = null;
        if (artifact.Scope.HasValue)
        {
            scope = JsonSerializer.Deserialize<AIPromptScope>(artifact.Scope.Value);
        }

        if (state.Entity == null)
        {
            // Create new prompt (ProfileId will be resolved in Pass 4)
            var prompt = new AIPrompt
            {
                Alias = artifact.Alias,
                Name = artifact.Name,
                Description = artifact.Description,
                Instructions = artifact.Instructions,
                ProfileId = null, // Will be resolved in Pass 4
                ContextIds = artifact.ContextIds.ToList(),
                Tags = artifact.Tags.ToList(),
                IsActive = artifact.IsActive,
                IncludeEntityContext = artifact.IncludeEntityContext,
                OptionCount = artifact.OptionCount,
                Scope = scope,
                CreatedByUserId = artifact.CreatedByUserId,
                ModifiedByUserId = artifact.ModifiedByUserId
            };

            state.Entity = await _promptService.SavePromptAsync(prompt, cancellationToken);
        }
        else
        {
            // Update existing prompt
            var prompt = state.Entity;
            prompt.Name = artifact.Name;
            prompt.Description = artifact.Description;
            prompt.Instructions = artifact.Instructions;
            prompt.ContextIds = artifact.ContextIds.ToList();
            prompt.Tags = artifact.Tags.ToList();
            prompt.IsActive = artifact.IsActive;
            prompt.IncludeEntityContext = artifact.IncludeEntityContext;
            prompt.OptionCount = artifact.OptionCount;
            prompt.Scope = scope;
            prompt.ModifiedByUserId = artifact.ModifiedByUserId;
            // ProfileId will be updated in Pass 4

            state.Entity = await _promptService.SavePromptAsync(prompt, cancellationToken);
        }
    }

    private async Task Pass4Async(
        ArtifactDeployState<AIPromptArtifact, AIPrompt> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        // Use base class helper to resolve optional ProfileId from ProfileUdi
        var profileId = await ResolveProfileIdAsync(artifact.ProfileUdi, cancellationToken);

        // Update prompt with resolved ProfileId
        var prompt = state.Entity!;
        prompt.ProfileId = profileId;

        state.Entity = await _promptService.SavePromptAsync(prompt, cancellationToken);
    }
}
