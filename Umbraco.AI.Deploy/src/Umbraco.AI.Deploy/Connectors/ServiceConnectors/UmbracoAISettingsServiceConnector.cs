using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Core;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for deploying AI settings (default profiles configuration).
/// Settings is a singleton entity with a fixed GUID.
/// </summary>
[UdiDefinition(UmbracoAIConstants.UdiEntityType.Settings, UdiType.GuidUdi)]
public class UmbracoAISettingsServiceConnector(
    IAISettingsService settingsService,
    IAIProfileService profileService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIEntityServiceConnectorBase<AISettingsArtifact, AISettings>(settingsAccessor)
{
    private readonly IAISettingsService _settingsService = settingsService;
    private readonly IAIProfileService _profileService = profileService;

    public override string UdiEntityType => UmbracoAIConstants.UdiEntityType.Settings;

    /// <summary>
    /// Settings uses Pass 2 and Pass 4 for profile dependency resolution.
    /// </summary>
    protected override int[] ProcessPasses => [2, 4];

    public override async Task<AISettings?> GetEntityAsync(Guid id, CancellationToken ct = default)
    {
        // Settings is a singleton, but we verify the ID matches
        if (id != AISettings.SettingsId)
            return null;

        return await _settingsService.GetSettingsAsync(ct);
    }

    public override IAsyncEnumerable<AISettings> GetEntitiesAsync(CancellationToken ct = default)
    {
        // Settings is a singleton - return a single instance
        return GetSingletonAsync(ct);
    }

    private async IAsyncEnumerable<AISettings> GetSingletonAsync(CancellationToken ct)
    {
        var settings = await _settingsService.GetSettingsAsync(ct);
        yield return settings;
    }

    public override string GetEntityName(AISettings entity) => "AI Settings";

    public override async Task<AISettingsArtifact?> GetArtifactAsync(
        GuidUdi? udi,
        AISettings? entity,
        CancellationToken ct = default)
    {
        if (entity == null)
            return null;

        var dependencies = new ArtifactDependencyCollection();

        // Add optional profile dependencies
        GuidUdi? chatProfileUdi = null;
        if (entity.DefaultChatProfileId.HasValue)
        {
            chatProfileUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Profile, entity.DefaultChatProfileId.Value);
            dependencies.Add(new UmbracoAIArtifactDependency(chatProfileUdi, ArtifactDependencyMode.Match));
        }

        GuidUdi? embeddingProfileUdi = null;
        if (entity.DefaultEmbeddingProfileId.HasValue)
        {
            embeddingProfileUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Profile, entity.DefaultEmbeddingProfileId.Value);
            dependencies.Add(new UmbracoAIArtifactDependency(embeddingProfileUdi, ArtifactDependencyMode.Match));
        }

        var artifact = new AISettingsArtifact(udi, dependencies)
        {
            DefaultChatProfileUdi = chatProfileUdi,
            DefaultEmbeddingProfileUdi = embeddingProfileUdi,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId
        };

        return artifact;
    }

    public override async Task ProcessAsync(
        ArtifactDeployState<AISettingsArtifact, AISettings> state,
        IDeployContext context,
        int pass,
        CancellationToken ct = default)
    {
        state.NextPass = GetNextPass(pass);

        switch (pass)
        {
            case 2:
                await Pass2Async(state, context, ct);
                break;
            case 4:
                await Pass4Async(state, context, ct);
                break;
        }
    }

    private async Task Pass2Async(
        ArtifactDeployState<AISettingsArtifact, AISettings> state,
        IDeployContext context,
        CancellationToken ct)
    {
        // Settings is a singleton - always exists, just update it
        var settings = await _settingsService.GetSettingsAsync(ct);

        // Pass 2: Set profile IDs to null (will be resolved in Pass 4)
        settings.DefaultChatProfileId = null;
        settings.DefaultEmbeddingProfileId = null;

        await _settingsService.SaveSettingsAsync(settings, ct);
    }

    private async Task Pass4Async(
        ArtifactDeployState<AISettingsArtifact, AISettings> state,
        IDeployContext context,
        CancellationToken ct)
    {
        var settings = await _settingsService.GetSettingsAsync(ct);

        // Resolve optional chat profile dependency
        if (state.Artifact.DefaultChatProfileUdi != null)
        {
            state.Artifact.DefaultChatProfileUdi.EnsureType(UmbracoAIConstants.UdiEntityType.Profile);
            var chatProfile = await _profileService.GetProfileAsync(state.Artifact.DefaultChatProfileUdi.Guid, ct);
            settings.DefaultChatProfileId = chatProfile?.Id;
        }
        else
        {
            settings.DefaultChatProfileId = null;
        }

        // Resolve optional embedding profile dependency
        if (state.Artifact.DefaultEmbeddingProfileUdi != null)
        {
            state.Artifact.DefaultEmbeddingProfileUdi.EnsureType(UmbracoAIConstants.UdiEntityType.Profile);
            var embeddingProfile = await _profileService.GetProfileAsync(state.Artifact.DefaultEmbeddingProfileUdi.Guid, ct);
            settings.DefaultEmbeddingProfileId = embeddingProfile?.Id;
        }
        else
        {
            settings.DefaultEmbeddingProfileId = null;
        }

        await _settingsService.SaveSettingsAsync(settings, ct);
    }
}
