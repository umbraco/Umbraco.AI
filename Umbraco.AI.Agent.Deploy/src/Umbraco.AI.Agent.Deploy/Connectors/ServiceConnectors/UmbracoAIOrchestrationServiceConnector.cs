using System.Runtime.CompilerServices;
using System.Text.Json;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Agent.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Agent.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for deploying Umbraco AI Orchestrations.
/// Handles export and import of orchestrations, including their graph and profile dependencies.
/// </summary>
[UdiDefinition(UmbracoAIAgentConstants.UdiEntityType.Orchestration, UdiType.GuidUdi)]
public class UmbracoAIOrchestrationServiceConnector(
    IAIOrchestrationService orchestrationService,
    IAIProfileService profileService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIProfileDependentEntityServiceConnectorBase<AIOrchestrationArtifact, AIOrchestration>(
        profileService,
        settingsAccessor)
{
    /// <inheritdoc />
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco AI Orchestrations";

    /// <inheritdoc />
    public override string UdiEntityType => UmbracoAIAgentConstants.UdiEntityType.Orchestration;

    /// <inheritdoc />
    public override Task<AIOrchestration?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => orchestrationService.GetOrchestrationAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIOrchestration> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var orchestrations = await orchestrationService.GetOrchestrationsAsync(cancellationToken);
        foreach (var orchestration in orchestrations)
        {
            yield return orchestration;
        }
    }

    /// <inheritdoc />
    public override string GetEntityName(AIOrchestration entity)
        => entity.Name;

    /// <inheritdoc />
    public override Task<AIOrchestrationArtifact?> GetArtifactAsync(
        GuidUdi udi,
        AIOrchestration? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null) return Task.FromResult<AIOrchestrationArtifact?>(null);

        var dependencies = new ArtifactDependencyCollection();

        // Use base class helper for optional profile dependency
        var profileUdi = AddProfileDependency(entity.ProfileId, dependencies);

        var artifact = new AIOrchestrationArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            ProfileUdi = profileUdi,
            SurfaceIds = entity.SurfaceIds.ToList(),
            Scope = entity.Scope != null ? JsonSerializer.SerializeToElement(entity.Scope) : null,
            Graph = JsonSerializer.SerializeToElement(entity.Graph),
            IsActive = entity.IsActive
        };

        return Task.FromResult<AIOrchestrationArtifact?>(artifact);
    }

    /// <inheritdoc />
    public override async Task ProcessAsync(
        ArtifactDeployState<AIOrchestrationArtifact, AIOrchestration> state,
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
        ArtifactDeployState<AIOrchestrationArtifact, AIOrchestration> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        // Resolve optional profile dependency
        var profileId = await ResolveProfileIdAsync(artifact.ProfileUdi, cancellationToken);

        // Deserialize Scope from JsonElement
        AIAgentScope? scope = null;
        if (artifact.Scope.HasValue)
        {
            scope = artifact.Scope.Value.Deserialize<AIAgentScope>();
        }

        // Deserialize Graph from JsonElement
        var graph = artifact.Graph.HasValue
            ? artifact.Graph.Value.Deserialize<AIOrchestrationGraph>()
            : new AIOrchestrationGraph();

        // Get or create orchestration entity
        var orchestration = state.Entity
            ?? new AIOrchestration
            {
                Id = artifact.Udi.Guid,
                Alias = artifact.Alias!,
                Name = artifact.Name
            };

        // Update properties from artifact
        orchestration.Alias = artifact.Alias!;
        orchestration.Name = artifact.Name;
        orchestration.Description = artifact.Description;
        orchestration.ProfileId = profileId;
        orchestration.SurfaceIds = artifact.SurfaceIds.ToList();
        orchestration.Scope = scope;
        orchestration.Graph = graph ?? new AIOrchestrationGraph();
        orchestration.IsActive = artifact.IsActive;

        state.Entity = await orchestrationService.SaveOrchestrationAsync(orchestration, cancellationToken);
    }
}
