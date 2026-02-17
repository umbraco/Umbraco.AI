using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Core;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

[UdiDefinition(UmbracoAIConstants.UdiEntityType.Profile, UdiType.GuidUdi)]
public class UmbracoAIProfileServiceConnector(
    IAIProfileService profileService,
    IAIConnectionService connectionService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIEntityServiceConnectorBase<AIProfileArtifact, AIProfile>(settingsAccessor)
{
    private readonly IAIProfileService _profileService = profileService;
    private readonly IAIConnectionService _connectionService = connectionService;

    protected override int[] ProcessPasses => [2, 4];
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];
    protected override string OpenUdiName => "All Umbraco AI Profiles";
    public override string UdiEntityType => UmbracoAIConstants.UdiEntityType.Profile;

    public override Task<AIProfile?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => _profileService.GetProfileAsync(id, cancellationToken);

    public override async IAsyncEnumerable<AIProfile> GetEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await _profileService.GetAllProfilesAsync(cancellationToken);
        foreach (var profile in profiles)
        {
            yield return profile;
        }
    }

    public override string GetEntityName(AIProfile entity)
        => entity.Name;

    public override Task<AIProfileArtifact?> GetArtifactAsync(
        GuidUdi? udi,
        AIProfile? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null) return Task.FromResult<AIProfileArtifact?>(null);

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
            case 4:
                await Pass4Async(state, context, cancellationToken);
                break;
        }
    }

    private async Task Pass2Async(
        ArtifactDeployState<AIProfileArtifact, AIProfile> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

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
                Alias = artifact.Alias,
                Name = artifact.Name,
                Capability = (AICapability)artifact.Capability,
                Model = modelRef,
                ConnectionId = Guid.Empty, // Placeholder - resolved in Pass 4
                Settings = settings,
                Tags = artifact.Tags.ToList(),
                CreatedByUserId = artifact.CreatedByUserId,
                ModifiedByUserId = artifact.ModifiedByUserId
            };

            state.Entity = await _profileService.SaveProfileAsync(profile, cancellationToken);
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
            // ConnectionId will be updated in Pass 4

            state.Entity = await _profileService.SaveProfileAsync(profile, cancellationToken);
        }
    }

    private async Task Pass4Async(
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

        // Update profile with resolved ConnectionId
        var profile = state.Entity!;
        profile.ConnectionId = connection.Id;

        state.Entity = await _profileService.SaveProfileAsync(profile, cancellationToken);
    }
}
