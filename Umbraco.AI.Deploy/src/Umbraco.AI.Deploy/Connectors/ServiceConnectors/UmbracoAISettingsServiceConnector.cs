using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

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
    /// <inheritdoc />
    public override string UdiEntityType => UmbracoAIConstants.UdiEntityType.Settings;

    /// <summary>
    /// Settings uses Pass 3 after profiles (Pass 2) to ensure that default profile dependencies can be resolved during deployment.
    /// </summary>
    protected override int[] ProcessPasses => [3];

    /// <inheritdoc />
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];

    /// <inheritdoc />
    protected override string OpenUdiName => "Umbraco AI Settings";

    /// <inheritdoc />
    public override async Task<AISettings?> GetEntityAsync(Guid id, CancellationToken ct = default)
    {
        // Settings is a singleton, but we verify the ID matches
        if (id != AISettings.SettingsId)
        {
            return null;
        }

        return await settingsService.GetSettingsAsync(ct);
    }

    /// <inheritdoc />
    public override IAsyncEnumerable<AISettings> GetEntitiesAsync(CancellationToken ct = default)
    {
        // Settings is a singleton - return a single instance
        return GetSingletonAsync(ct);
    }

    private async IAsyncEnumerable<AISettings> GetSingletonAsync(CancellationToken ct)
    {
        var settings = await settingsService.GetSettingsAsync(ct);
        yield return settings;
    }

    /// <inheritdoc />
    public override string GetEntityName(AISettings entity) => "AI Settings";

    /// <inheritdoc />
    public override async Task<AISettingsArtifact?> GetArtifactAsync(
        GuidUdi udi,
        AISettings? entity,
        CancellationToken ct = default)
    {
        if (entity == null)
        {
            return null;
        }

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

    /// <inheritdoc />
    public override async Task ProcessAsync(
        ArtifactDeployState<AISettingsArtifact, AISettings> state,
        IDeployContext context,
        int pass,
        CancellationToken ct = default)
    {
        state.NextPass = GetNextPass(pass);

        switch (pass)
        {
            case 3:
                await Pass3Async(state, context, ct);
                break;
        }
    }

    private async Task Pass3Async(
        ArtifactDeployState<AISettingsArtifact, AISettings> state,
        IDeployContext context,
        CancellationToken ct)
    {
        var settings = await settingsService.GetSettingsAsync(ct);

        // Resolve optional chat profile dependency
        if (state.Artifact.DefaultChatProfileUdi != null)
        {
            state.Artifact.DefaultChatProfileUdi.EnsureType(UmbracoAIConstants.UdiEntityType.Profile);
            var chatProfile = await profileService.GetProfileAsync(state.Artifact.DefaultChatProfileUdi.Guid, ct);
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
            var embeddingProfile = await profileService.GetProfileAsync(state.Artifact.DefaultEmbeddingProfileUdi.Guid, ct);
            settings.DefaultEmbeddingProfileId = embeddingProfile?.Id;
        }
        else
        {
            settings.DefaultEmbeddingProfileId = null;
        }

        await settingsService.SaveSettingsAsync(settings, ct);
    }
}
