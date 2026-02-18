using System.Runtime.CompilerServices;
using System.Text.Json;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for Umbraco AI Profiles, responsible for deploying AIProfile entities based on AIProfileArtifact definitions.
/// This connector handles the creation and updating of AI Profiles, including resolving dependencies on AI Connections.
/// The deployment process is split into multiple passes to ensure that dependencies are resolved in the correct order
/// (e.g., Connections must be deployed before Profiles that depend on them).
/// </summary>
[UdiDefinition(UmbracoAIConstants.UdiEntityType.Profile, UdiType.GuidUdi)]
public class UmbracoAIProfileServiceConnector(
    IAIProfileService profileService,
    IAIConnectionService connectionService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIEntityServiceConnectorBase<AIProfileArtifact, AIProfile>(settingsAccessor)
{
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
        => profileService.GetProfileAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIProfile> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var profiles = await profileService.GetAllProfilesAsync(cancellationToken);
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

        var artifact = new AIProfileArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            Capability = (int)entity.Capability,
            ModelProviderId = entity.Model.ProviderId,
            ModelModelId = entity.Model.ModelId,
            ConnectionUdi = connectionUdi,
            Settings = entity.Settings != null ? JsonSerializer.SerializeToElement(entity.Settings) : null,
            Tags = entity.Tags.ToList(),
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId,
            Version = entity.Version
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

        var connection = await connectionService.GetConnectionAsync(artifact.ConnectionUdi.Guid, cancellationToken);
        if (connection == null)
        {
            throw new InvalidOperationException($"Connection with ID {artifact.ConnectionUdi.Guid} not found. Ensure the connection is deployed before the profile.");
        }

        // Deserialize settings from JsonElement based on capability
        IAIProfileSettings? settings = null;
        if (artifact.Settings.HasValue)
        {
            var capability = (AICapability)artifact.Capability;
            settings = capability switch
            {
                AICapability.Chat => JsonSerializer.Deserialize<AIChatProfileSettings>(artifact.Settings.Value),
                AICapability.Embedding => JsonSerializer.Deserialize<AIEmbeddingProfileSettings>(artifact.Settings.Value),
                _ => null
            };
        }

        // Create AIModelRef from artifact properties
        var modelRef = new AIModelRef(artifact.ModelProviderId, artifact.ModelModelId);

        if (state.Entity == null)
        {
            // Create new profile (ConnectionId will be resolved in Pass 4)
            // For now, use a placeholder - we'll update it in Pass 4
            var profile = new AIProfile
            {
                ConnectionId = connection.Id, // Set ConnectionId to resolved value
                Alias = artifact.Alias!,
                Name = artifact.Name,
                Capability = (AICapability)artifact.Capability,
                Model = modelRef,
                Settings = settings,
                Tags = artifact.Tags.ToList(),
                CreatedByUserId = artifact.CreatedByUserId,
                ModifiedByUserId = artifact.ModifiedByUserId
            };

            state.Entity = await profileService.SaveProfileAsync(profile, cancellationToken);
        }
        else
        {
            // Update existing profile
            var profile = state.Entity;

            // Validate that capability hasn't changed (it's init-only, so we can't change it)
            if (profile.Capability != (AICapability)artifact.Capability)
            {
                throw new InvalidOperationException(
                    $"Cannot change profile capability from {profile.Capability} to {(AICapability)artifact.Capability}. " +
                    "Profile capability is immutable after creation.");
            }

            // Update mutable properties
            profile.Name = artifact.Name;
            profile.Model = modelRef;
            profile.Settings = settings;
            profile.Tags = artifact.Tags.ToList();
            profile.ModifiedByUserId = artifact.ModifiedByUserId;
            profile.ConnectionId = connection.Id;

            state.Entity = await profileService.SaveProfileAsync(profile, cancellationToken);
        }
    }
}
