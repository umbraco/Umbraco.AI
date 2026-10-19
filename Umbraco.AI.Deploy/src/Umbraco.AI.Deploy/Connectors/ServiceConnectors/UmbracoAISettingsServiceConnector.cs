using Umbraco.AI.Core.Models;
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

        GuidUdi? speechToTextProfileUdi = null;
        if (entity.DefaultSpeechToTextProfileId.HasValue)
        {
            speechToTextProfileUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Profile, entity.DefaultSpeechToTextProfileId.Value);
            dependencies.Add(new UmbracoAIArtifactDependency(speechToTextProfileUdi, ArtifactDependencyMode.Match));
        }

        GuidUdi? imageGenerationProfileUdi = null;
        if (entity.DefaultImageGenerationProfileId.HasValue)
        {
            imageGenerationProfileUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Profile, entity.DefaultImageGenerationProfileId.Value);
            dependencies.Add(new UmbracoAIArtifactDependency(imageGenerationProfileUdi, ArtifactDependencyMode.Match));
        }

        GuidUdi? decisionProfileUdi = null;
        if (entity.DefaultDecisionProfileId.HasValue)
        {
            decisionProfileUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Profile, entity.DefaultDecisionProfileId.Value);
            dependencies.Add(new UmbracoAIArtifactDependency(decisionProfileUdi, ArtifactDependencyMode.Match));
        }

        GuidUdi? classifierChatProfileUdi = null;
        if (entity.ClassifierChatProfileId.HasValue)
        {
            classifierChatProfileUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Profile, entity.ClassifierChatProfileId.Value);
            dependencies.Add(new UmbracoAIArtifactDependency(classifierChatProfileUdi, ArtifactDependencyMode.Match));
        }

        var artifact = new AISettingsArtifact(udi, dependencies)
        {
            DefaultChatProfileUdi = chatProfileUdi,
            DefaultEmbeddingProfileUdi = embeddingProfileUdi,
            DefaultSpeechToTextProfileUdi = speechToTextProfileUdi,
            DefaultImageGenerationProfileUdi = imageGenerationProfileUdi,
            DefaultDecisionProfileUdi = decisionProfileUdi,
            ClassifierChatProfileUdi = classifierChatProfileUdi,
            DisclosureNoticeMode = entity.DisclosureNoticeMode.ToString()
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

        // Resolve each optional default-profile dependency. A profile that no longer exists, or whose
        // capability no longer matches the slot (e.g. an artifact edited by hand, or the profile's
        // capability changed on the source environment after export), is dropped rather than failing
        // the whole import - profiles deploy in an earlier pass (2) than settings (3), so this is the
        // same "best effort" treatment as a dependency that never resolved.
        settings.DefaultChatProfileId = await ResolveDefaultProfileIdAsync(state.Artifact.DefaultChatProfileUdi, AICapability.Chat, ct);
        settings.DefaultEmbeddingProfileId = await ResolveDefaultProfileIdAsync(state.Artifact.DefaultEmbeddingProfileUdi, AICapability.Embedding, ct);
        settings.DefaultSpeechToTextProfileId = await ResolveDefaultProfileIdAsync(state.Artifact.DefaultSpeechToTextProfileUdi, AICapability.SpeechToText, ct);
        settings.DefaultImageGenerationProfileId = await ResolveDefaultProfileIdAsync(state.Artifact.DefaultImageGenerationProfileUdi, AICapability.ImageGeneration, ct);
        settings.DefaultDecisionProfileId = await ResolveDefaultProfileIdAsync(state.Artifact.DefaultDecisionProfileUdi, AICapability.Decision, ct);
        settings.ClassifierChatProfileId = await ResolveDefaultProfileIdAsync(state.Artifact.ClassifierChatProfileUdi, AICapability.Chat, ct);

        // Artifacts written before this setting existed carry no value, so keep the target's own.
        if (Enum.TryParse<AIDisclosureNoticeMode>(state.Artifact.DisclosureNoticeMode, true, out var disclosureNoticeMode)
            && Enum.IsDefined(disclosureNoticeMode))
        {
            settings.DisclosureNoticeMode = disclosureNoticeMode;
        }

        await settingsService.SaveSettingsAsync(settings, ct);
    }

    /// <summary>
    /// Resolves a default-profile slot's dependency UDI to a profile ID, or null if the UDI is unset,
    /// the profile no longer exists, or the profile's capability doesn't match what the slot requires.
    /// </summary>
    private async Task<Guid?> ResolveDefaultProfileIdAsync(GuidUdi? profileUdi, AICapability requiredCapability, CancellationToken ct)
    {
        if (profileUdi is null)
        {
            return null;
        }

        profileUdi.EnsureType(UmbracoAIConstants.UdiEntityType.Profile);
        var profile = await profileService.GetProfileAsync(profileUdi.Guid, ct);
        return profile is not null && profile.Capability == requiredCapability ? profile.Id : null;
    }
}
