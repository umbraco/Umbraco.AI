using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for Umbraco AI Profiles, responsible for deploying AIProfile entities based on AIProfileArtifact definitions.
/// This connector handles the creation and updating of AI Profiles, including resolving dependencies on AI Connections.
/// The deployment process is split into multiple passes to ensure that dependencies are resolved in the correct order
/// (e.g., Connections must be deployed before Profiles that depend on them).
/// </summary>
[UdiDefinition(UmbracoAIConstants.UdiEntityType.Profile, UdiType.GuidUdi)]
public class UmbracoAIProfileServiceConnector
    : UmbracoAIEntityServiceConnectorBase<AIProfileArtifact, AIProfile>
{
    private readonly IAIProfileService _profileService;
    private readonly IAIConnectionService _connectionService;
    private readonly AIProviderCollection _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIProfileServiceConnector"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an AIProviderCollection. Will be removed in v20.")]
    public UmbracoAIProfileServiceConnector(
        IAIProfileService profileService,
        IAIConnectionService connectionService,
        UmbracoAIDeploySettingsAccessor settingsAccessor)
        : this(
            profileService,
            connectionService,
            settingsAccessor,
            StaticServiceProvider.Instance.GetRequiredService<AIProviderCollection>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIProfileServiceConnector"/> class.
    /// </summary>
    public UmbracoAIProfileServiceConnector(
        IAIProfileService profileService,
        IAIConnectionService connectionService,
        UmbracoAIDeploySettingsAccessor settingsAccessor,
        AIProviderCollection providers)
        : base(settingsAccessor)
    {
        _profileService = profileService;
        _connectionService = connectionService;
        _providers = providers;
    }

    /// <inheritdoc />
    protected override int[] ProcessPasses => [2];

    /// <inheritdoc />
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco AI Profiles";

    /// <inheritdoc />
    public override string UdiEntityType => UmbracoAIConstants.UdiEntityType.Profile;

    /// <inheritdoc />
    public override Task<AIProfile?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => _profileService.GetProfileAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIProfile> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var profiles = await _profileService.GetAllProfilesAsync(cancellationToken);
        foreach (var profile in profiles)
        {
            yield return profile;
        }
    }

    /// <inheritdoc />
    public override string GetEntityName(AIProfile entity)
        => entity.Name;

    /// <inheritdoc />
    public override Task<AIProfileArtifact?> GetArtifactAsync(
        GuidUdi udi,
        AIProfile? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            return Task.FromResult<AIProfileArtifact?>(null);
        }

        var dependencies = new ArtifactDependencyCollection();

        // Add connection dependency
        var connectionUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Connection, entity.ConnectionId);
        dependencies.Add(new UmbracoAIArtifactDependency(connectionUdi, ArtifactDependencyMode.Match));

        // Add context and guardrail dependencies from chat profile settings
        if (entity.Settings is AIChatProfileSettings chatSettings)
        {
            foreach (var contextId in chatSettings.ContextIds)
            {
                var contextUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Context, contextId);
                dependencies.Add(new UmbracoAIArtifactDependency(contextUdi, ArtifactDependencyMode.Match));
            }

            foreach (var guardrailId in chatSettings.GuardrailIds)
            {
                var guardrailUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Guardrail, guardrailId);
                dependencies.Add(new UmbracoAIArtifactDependency(guardrailUdi, ArtifactDependencyMode.Match));
            }
        }

        object? capabilitySettings = UmbracoAISensitiveSettingsFilter.RemoveSensitiveLiterals(
            entity.CapabilitySettings,
            GetCapabilitySettingsSchema(entity.Model.ProviderId, entity.Capability));

        var artifact = new AIProfileArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            Capability = (int)entity.Capability,
            ModelProviderId = entity.Model.ProviderId,
            ModelModelId = entity.Model.ModelId,
            ConnectionUdi = connectionUdi,
            // Serialize against the runtime type (Settings is declared as the marker interface
            // IAIProfileSettings, which would otherwise serialize as an empty object) using the
            // core serializer options so the artifact round-trips through AIProfileSettingsSerializer.
            // The provider-declared bag is already shaped by the provider's schema, so it round-trips as
            // whatever it holds — a JsonElement read from storage, or the object the API supplied —
            // minus any sensitive literal, which never leaves this environment.
            CapabilitySettings = capabilitySettings != null
                ? JsonSerializer.SerializeToElement(capabilitySettings, capabilitySettings.GetType(), Umbraco.AI.Core.Constants.DefaultJsonSerializerOptions)
                : null,
            Settings = entity.Settings != null
                ? JsonSerializer.SerializeToElement(entity.Settings, entity.Settings.GetType(), Umbraco.AI.Core.Constants.DefaultJsonSerializerOptions)
                : null,
            Tags = entity.Tags.ToList()
        };

        return Task.FromResult<AIProfileArtifact?>(artifact);
    }

    /// <inheritdoc />
    public override async Task ProcessAsync(
        ArtifactDeployState<AIProfileArtifact, AIProfile> state,
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
        }
    }

    private async Task Pass2Async(
        ArtifactDeployState<AIProfileArtifact, AIProfile> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        // Resolve ConnectionId from ConnectionUdi
        artifact.ConnectionUdi.EnsureType(UmbracoAIConstants.UdiEntityType.Connection);

        var connection = await _connectionService.GetConnectionAsync(artifact.ConnectionUdi.Guid, cancellationToken);
        if (connection == null)
        {
            throw new InvalidOperationException($"Connection with ID {artifact.ConnectionUdi.Guid} not found. Ensure the connection is deployed before the profile.");
        }

        // Deserialize settings from JsonElement based on capability.
        // Delegate to the core serializer (rather than duplicating the capability switch here)
        // so support for every capability — including ImageGeneration — stays in sync with the core.
        IAIProfileSettings? settings = null;
        if (artifact.Settings.HasValue)
        {
            var capability = (AICapability)artifact.Capability;
            settings = AIProfileSettingsSerializer.Deserialize(capability, artifact.Settings.Value.GetRawText());
        }

        // Create AIModelRef from artifact properties
        var modelRef = new AIModelRef(artifact.ModelProviderId, artifact.ModelModelId);

        // Get or create profile entity
        var profile = state.Entity
            ?? new AIProfile(artifact.Udi.Guid)
            {
                Alias = artifact.Alias!,
                Name = artifact.Name,
                Capability = (AICapability)artifact.Capability,
                ConnectionId = connection.Id
            };

        // Update mutable properties
        profile.Alias = artifact.Alias!;
        profile.Name = artifact.Name;
        profile.ConnectionId = connection.Id;
        profile.Model = modelRef;
        profile.Settings = settings;
        profile.CapabilitySettings = UmbracoAISensitiveSettingsFilter.ImportSettings(
            artifact.CapabilitySettings,
            state.Entity?.CapabilitySettings,
            GetCapabilitySettingsSchema(artifact.ModelProviderId, (AICapability)artifact.Capability));
        profile.Tags = artifact.Tags.ToList();

        state.Entity = await _profileService.SaveProfileAsync(profile, cancellationToken);
    }

    private AIEditableModelSchema? GetCapabilitySettingsSchema(string providerId, AICapability capability)
        => _providers.GetById(providerId)?.GetCapabilitySettingsSchema(capability);
}
