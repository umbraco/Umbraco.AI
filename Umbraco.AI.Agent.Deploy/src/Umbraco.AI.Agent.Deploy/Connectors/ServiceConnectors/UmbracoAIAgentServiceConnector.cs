using System.Runtime.CompilerServices;
using System.Text.Json;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Agent.Deploy.Artifacts;
using Umbraco.AI.Deploy;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Agent.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for deploying Umbraco AI Agents. Handles export and import of agents, including their dependencies on profiles.
/// </summary>
[UdiDefinition(UmbracoAIAgentConstants.UdiEntityType.Agent, UdiType.GuidUdi)]
public class UmbracoAIAgentServiceConnector(
    IAIAgentService agentService,
    IAIProfileService profileService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIProfileDependentEntityServiceConnectorBase<AIAgentArtifact, AIAgent>(
        profileService,
        settingsAccessor)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <inheritdoc />
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco AI Agents";

    /// <inheritdoc />
    public override string UdiEntityType => UmbracoAIAgentConstants.UdiEntityType.Agent;

    /// <inheritdoc />
    public override Task<AIAgent?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => agentService.GetAgentAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIAgent> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var agents = await agentService.GetAgentsAsync(cancellationToken);
        foreach (var agent in agents)
        {
            yield return agent;
        }
    }

    /// <inheritdoc />
    public override string GetEntityName(AIAgent entity)
        => entity.Name;

    /// <inheritdoc />
    public override Task<AIAgentArtifact?> GetArtifactAsync(
        GuidUdi udi,
        AIAgent? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null) return Task.FromResult<AIAgentArtifact?>(null);

        var dependencies = new ArtifactDependencyCollection();

        // Use base class helper for optional profile dependency
        var profileUdi = AddProfileDependency(entity.ProfileId, dependencies);

        // Add guardrail dependencies from standard agent config
        if (entity.Config is AIStandardAgentConfig standardConfig)
        {
            foreach (var guardrailId in standardConfig.GuardrailIds)
            {
                var guardrailUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Guardrail, guardrailId);
                dependencies.Add(new UmbracoAIArtifactDependency(guardrailUdi, ArtifactDependencyMode.Match));
            }
        }

        // Serialize config to JSON
        string? configJson = null;
        if (entity.Config is not null)
        {
            configJson = JsonSerializer.Serialize(entity.Config, entity.Config.GetType(), JsonOptions);
        }

        var artifact = new AIAgentArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            ProfileUdi = profileUdi,
            AgentType = entity.AgentType.ToString(),
            Config = configJson,
            SurfaceIds = entity.SurfaceIds.ToList(),
            Scope = entity.Scope != null ? JsonSerializer.SerializeToElement(entity.Scope) : null,
            IsActive = entity.IsActive
        };

        return Task.FromResult<AIAgentArtifact?>(artifact);
    }

    /// <inheritdoc />
    public override async Task ProcessAsync(
        ArtifactDeployState<AIAgentArtifact, AIAgent> state,
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
        ArtifactDeployState<AIAgentArtifact, AIAgent> state,
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

        // Parse agent type
        var agentType = Enum.TryParse<AIAgentType>(artifact.AgentType, ignoreCase: true, out var parsed)
            ? parsed
            : AIAgentType.Standard;

        // Deserialize config based on agent type
        IAIAgentConfig? config = DeserializeConfig(agentType, artifact.Config);

        // Get or create agent entity
        var agent = state.Entity
            ?? new AIAgent
            {
                Id = artifact.Udi.Guid,
                Alias = artifact.Alias!,
                Name = artifact.Name,
                AgentType = agentType,
            };

        // Update properties from artifact
        agent.Alias = artifact.Alias!;
        agent.Name = artifact.Name;
        agent.Description = artifact.Description;
        agent.ProfileId = profileId;
        agent.Config = config;
        agent.SurfaceIds = artifact.SurfaceIds.ToList();
        agent.Scope = scope;
        agent.IsActive = artifact.IsActive;

        state.Entity = await agentService.SaveAgentAsync(agent, cancellationToken);
    }

    private static IAIAgentConfig? DeserializeConfig(AIAgentType agentType, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return agentType switch
            {
                AIAgentType.Standard => new AIStandardAgentConfig(),
                AIAgentType.Orchestrated => new AIOrchestratedAgentConfig(),
                _ => null,
            };
        }

        return agentType switch
        {
            AIAgentType.Standard => JsonSerializer.Deserialize<AIStandardAgentConfig>(json, JsonOptions),
            AIAgentType.Orchestrated => JsonSerializer.Deserialize<AIOrchestratedAgentConfig>(json, JsonOptions),
            _ => null,
        };
    }
}
