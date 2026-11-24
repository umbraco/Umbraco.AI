using System.Runtime.CompilerServices;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Deploy;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Deploy.Artifacts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Prompt.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for Umbraco AI Prompts, responsible for connecting the deployment process to the IAIPromptService to retrieve and save prompts during deployment.
/// </summary>
[UdiDefinition(UmbracoAIPromptConstants.UdiEntityType.Prompt, UdiType.GuidUdi)]
public class UmbracoAIPromptServiceConnector
    : UmbracoAIProfileDependentEntityServiceConnectorBase<AIPromptArtifact, AIPrompt>
{
    private readonly IAIPromptService _promptService;
    private readonly IAIPromptUnrestrictedReader _promptReader;
    private readonly IUserGroupService _userGroupService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIPromptServiceConnector"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an IUserGroupService. Will be removed in v20.")]
    public UmbracoAIPromptServiceConnector(
        IAIPromptService promptService,
        IAIProfileService profileService,
        UmbracoAIDeploySettingsAccessor settingsAccessor)
        : this(
            promptService,
            profileService,
            settingsAccessor,
            StaticServiceProvider.Instance.GetRequiredService<IUserGroupService>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIPromptServiceConnector"/> class.
    /// </summary>
    public UmbracoAIPromptServiceConnector(
        IAIPromptService promptService,
        IAIProfileService profileService,
        UmbracoAIDeploySettingsAccessor settingsAccessor,
        IUserGroupService userGroupService)
        : this(
            promptService,
            profileService,
            settingsAccessor,
            userGroupService,
            StaticServiceProvider.Instance.GetRequiredService<IAIPromptUnrestrictedReader>())
    {
    }

    // Deploy runs with no signed-in user, so export and import read prompts through the unrestricted
    // reader: the per-user filter on IAIPromptService would hide limited prompts. The reader is internal,
    // so the public constructor above resolves it rather than taking it.
    internal UmbracoAIPromptServiceConnector(
        IAIPromptService promptService,
        IAIProfileService profileService,
        UmbracoAIDeploySettingsAccessor settingsAccessor,
        IUserGroupService userGroupService,
        IAIPromptUnrestrictedReader promptReader)
        : base(profileService, settingsAccessor)
    {
        _promptService = promptService;
        _promptReader = promptReader;
        _userGroupService = userGroupService;
    }

    /// <inheritdoc />
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco AI Prompts";

    /// <inheritdoc />
    public override string UdiEntityType => UmbracoAIPromptConstants.UdiEntityType.Prompt;

    /// <inheritdoc />
    public override Task<AIPrompt?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => _promptReader.GetPromptUnrestrictedAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIPrompt> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prompts = await _promptReader.GetPromptsUnrestrictedAsync(cancellationToken);
        foreach (var prompt in prompts)
        {
            yield return prompt;
        }
    }

    /// <inheritdoc />
    public override string GetEntityName(AIPrompt entity)
        => entity.Name;

    /// <inheritdoc />
    public override async Task<AIPromptArtifact?> GetArtifactAsync(
        GuidUdi udi,
        AIPrompt? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null) return null;

        var dependencies = new ArtifactDependencyCollection();

        // Use base class helper for optional profile dependency
        var profileUdi = AddProfileDependency(entity.ProfileId, dependencies);

        // Add context dependencies
        foreach (var contextId in entity.ContextIds)
        {
            var contextUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Context, contextId);
            dependencies.Add(new UmbracoAIArtifactDependency(contextUdi, ArtifactDependencyMode.Match));
        }

        // Add guardrail dependencies
        foreach (var guardrailId in entity.GuardrailIds)
        {
            var guardrailUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Guardrail, guardrailId);
            dependencies.Add(new UmbracoAIArtifactDependency(guardrailUdi, ArtifactDependencyMode.Match));
        }

        var artifact = new AIPromptArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            Instructions = entity.Instructions,
            ProfileUdi = profileUdi,
            ContextIds = entity.ContextIds.ToList(),
            GuardrailIds = entity.GuardrailIds.ToList(),
            AllowedUserGroups = await GetUserGroupAliasesAsync(entity.AllowedUserGroupIds),
            Tags = entity.Tags.ToList(),
            IsActive = entity.IsActive,
            IncludeEntityContext = entity.IncludeEntityContext,
            OptionCount = entity.OptionCount,
            Scope = entity.Scope != null ? JsonSerializer.SerializeToElement(entity.Scope) : null
        };

        return artifact;
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
            scope = artifact.Scope.Value.Deserialize<AIPromptScope>();
        }

        // Resolve the allowed user groups before anything is saved, so a missing group fails the import
        var allowedUserGroupIds = await GetTargetUserGroupIdsAsync(artifact.AllowedUserGroups, artifact.Alias);

        // Get or create prompt entity
        var prompt = state.Entity
            ?? new AIPrompt(artifact.Udi.Guid)
            {
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
        prompt.GuardrailIds = artifact.GuardrailIds.ToList();
        prompt.AllowedUserGroupIds = allowedUserGroupIds;
        prompt.Tags = artifact.Tags.ToList();
        prompt.IsActive = artifact.IsActive;
        prompt.IncludeEntityContext = artifact.IncludeEntityContext;
        prompt.OptionCount = artifact.OptionCount;
        prompt.Scope = scope;

        state.Entity = await _promptService.SavePromptAsync(prompt, cancellationToken);
    }

    /// <summary>
    /// Maps user group keys to aliases, for export. A key that matches no group here is kept as it is,
    /// so the import on the other side names it in its error rather than dropping the limit.
    /// </summary>
    private async Task<List<string>> GetUserGroupAliasesAsync(IEnumerable<Guid> userGroupIds)
    {
        var aliases = new List<string>();
        foreach (var userGroupId in userGroupIds)
        {
            var userGroup = await _userGroupService.GetAsync(userGroupId);
            aliases.Add(userGroup?.Alias ?? userGroupId.ToString());
        }

        return aliases;
    }

    /// <summary>
    /// Maps user group aliases to this environment's keys, for import. A missing group throws: skipping
    /// it would widen who can use the prompt, which an administrator limited on purpose.
    /// </summary>
    private async Task<List<Guid>> GetTargetUserGroupIdsAsync(IEnumerable<string> aliases, string? promptAlias)
    {
        var userGroupIds = new List<Guid>();
        foreach (var alias in aliases)
        {
            var userGroup = await _userGroupService.GetAsync(alias)
                ?? throw new InvalidOperationException(
                    $"Prompt '{promptAlias}' is limited to user group '{alias}', which doesn't exist in this environment. "
                    + "Create a user group with that alias (or, if the value is a key, add the group to the source and export again), "
                    + "then deploy the prompt again.");
            userGroupIds.Add(userGroup.Key);
        }

        return userGroupIds;
    }
}
