using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Agent.Deploy.Artifacts;
using Umbraco.AI.Deploy;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Agent.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for deploying Umbraco AI Agents. Handles export and import of agents, including their dependencies on profiles.
/// </summary>
/// <remarks>
/// A standard agent's user group permissions are keyed by user group in the artifact by <em>alias</em>, not
/// by key: a user group created separately on each environment has a different key on each, and Deploy
/// doesn't transfer user groups. Import maps each alias back to the target's own group.
/// </remarks>
[UdiDefinition(UmbracoAIAgentConstants.UdiEntityType.Agent, UdiType.GuidUdi)]
public class UmbracoAIAgentServiceConnector
    : UmbracoAIProfileDependentEntityServiceConnectorBase<AIAgentArtifact, AIAgent>
{
    private const string UserGroupPermissionsProperty = "userGroupPermissions";

    private readonly IAIAgentService _agentService;
    private readonly IUserGroupService _userGroupService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIAgentServiceConnector"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an IUserGroupService. Will be removed in v20.")]
    public UmbracoAIAgentServiceConnector(
        IAIAgentService agentService,
        IAIProfileService profileService,
        UmbracoAIDeploySettingsAccessor settingsAccessor)
        : this(
            agentService,
            profileService,
            settingsAccessor,
            StaticServiceProvider.Instance.GetRequiredService<IUserGroupService>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIAgentServiceConnector"/> class.
    /// </summary>
    public UmbracoAIAgentServiceConnector(
        IAIAgentService agentService,
        IAIProfileService profileService,
        UmbracoAIDeploySettingsAccessor settingsAccessor,
        IUserGroupService userGroupService)
        : base(profileService, settingsAccessor)
    {
        _agentService = agentService;
        _userGroupService = userGroupService;
    }
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Maximum number of starter prompts an agent can carry (mirrors <c>AIAgentService</c>'s own
    /// guard). Import clamps to this count rather than throwing — a transfer from another version
    /// line, or after a future cap change, should never fail the whole agent over a presentation
    /// rule.
    /// </summary>
    private const int MaxStarterPrompts = 4;

    /// <summary>
    /// Maximum length, in characters, of a single starter prompt (mirrors <c>AIAgentService</c>'s
    /// own guard). Truncated on import for the same reason the count is clamped.
    /// </summary>
    private const int MaxStarterPromptLength = 200;

    /// <inheritdoc />
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco AI Agents";

    /// <inheritdoc />
    public override string UdiEntityType => UmbracoAIAgentConstants.UdiEntityType.Agent;

    /// <inheritdoc />
    public override Task<AIAgent?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => _agentService.GetAgentAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIAgent> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var agents = await _agentService.GetAgentsAsync(cancellationToken);
        foreach (var agent in agents)
        {
            yield return agent;
        }
    }

    /// <inheritdoc />
    public override string GetEntityName(AIAgent entity)
        => entity.Name;

    /// <inheritdoc />
    public override async Task<AIAgentArtifact?> GetArtifactAsync(
        GuidUdi udi,
        AIAgent? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null) return null;

        var dependencies = new ArtifactDependencyCollection();

        // Use base class helper for optional profile dependency
        var profileUdi = AddProfileDependency(entity.ProfileId, dependencies);

        // Add guardrail dependencies (available for all agent types)
        foreach (var guardrailId in entity.GuardrailIds)
        {
            var guardrailUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Guardrail, guardrailId);
            dependencies.Add(new UmbracoAIArtifactDependency(guardrailUdi, ArtifactDependencyMode.Match));
        }

        // Add context dependencies (standard agents only)
        if (entity.Config is AIStandardAgentConfig standardConfig)
        {
            foreach (var contextId in standardConfig.ContextIds)
            {
                var contextUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Context, contextId);
                dependencies.Add(new UmbracoAIArtifactDependency(contextUdi, ArtifactDependencyMode.Match));
            }
        }

        // Serialize config to JSON
        string? configJson = null;
        if (entity.Config is not null)
        {
            configJson = JsonSerializer.Serialize(entity.Config, entity.Config.GetType(), JsonOptions);

            if (entity.Config is AIStandardAgentConfig)
            {
                configJson = await KeyUserGroupPermissionsByAliasAsync(configJson);
            }
        }

        var artifact = new AIAgentArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            ProfileUdi = profileUdi,
            AgentType = entity.AgentType.ToString(),
            Config = configJson,
            GuardrailIds = entity.GuardrailIds.ToList(),
            SurfaceIds = entity.SurfaceIds.ToList(),
            Scope = entity.Scope != null ? JsonSerializer.SerializeToElement(entity.Scope) : null,
            StarterPrompts = entity.StarterPrompts.ToList(),
            IsActive = entity.IsActive
        };

        return artifact;
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

        // Deserialize config based on agent type, mapping user group aliases to this environment's groups
        var configJson = agentType == AIAgentType.Standard && !string.IsNullOrWhiteSpace(artifact.Config)
            ? await KeyUserGroupPermissionsByTargetGroupAsync(artifact.Config, artifact.Alias)
            : artifact.Config;
        IAIAgentConfig? config = DeserializeConfig(agentType, configJson);

        // Get or create agent entity
        var agent = state.Entity
            ?? new AIAgent(artifact.Udi.Guid)
            {
                Alias = artifact.Alias!,
                Name = artifact.Name,
                AgentType = agentType,
            };

        // Update properties from artifact
        agent.Alias = artifact.Alias!;
        agent.Name = artifact.Name;
        agent.Description = artifact.Description;
        agent.ProfileId = profileId;
        agent.GuardrailIds = artifact.GuardrailIds.ToList();
        agent.Config = config;
        agent.SurfaceIds = artifact.SurfaceIds.ToList();
        agent.Scope = scope;
        // Clamp rather than throw: an artifact breaching either cap — from another version line, or
        // after a future cap change — must not fail the whole transfer over this alone. Both of
        // SaveAgentAsync's guards are mirrored here, count and length, or the one left unclamped
        // would throw right below and defeat the point.
        agent.StarterPrompts = artifact.StarterPrompts
            .Take(MaxStarterPrompts)
            .Select(p => p.Prompt.Length > MaxStarterPromptLength
                ? p with { Prompt = p.Prompt[..MaxStarterPromptLength] }
                : p)
            .ToList();
        agent.IsActive = artifact.IsActive;

        state.Entity = await _agentService.SaveAgentAsync(agent, cancellationToken);
    }

    /// <summary>
    /// Re-keys a standard config's user group permissions from user group key to alias, for export.
    /// A key that matches no group here is left as it is.
    /// </summary>
    private async Task<string> KeyUserGroupPermissionsByAliasAsync(string configJson)
    {
        JsonObject? config = JsonNode.Parse(configJson)?.AsObject();
        if (config?[UserGroupPermissionsProperty] is not JsonObject permissions)
        {
            return configJson;
        }

        var rekeyed = new JsonObject();
        foreach ((string key, JsonNode? value) in permissions.ToList())
        {
            permissions.Remove(key);
            var userGroup = Guid.TryParse(key, out Guid userGroupKey)
                ? await _userGroupService.GetAsync(userGroupKey)
                : null;
            rekeyed[userGroup?.Alias ?? key] = value;
        }

        config[UserGroupPermissionsProperty] = rekeyed;
        return config.ToJsonString();
    }

    /// <summary>
    /// Re-keys a standard config's user group permissions to this environment's user group keys, for import.
    /// </summary>
    /// <remarks>
    /// Entries are keyed by alias; artifacts written by older versions are keyed by the source's user group
    /// key, which is used as-is only when a group with that key exists here. An entry that matches no group
    /// here is dropped when it only allows tools (it would grant nobody anything), but fails the import when
    /// it denies any: silently losing a deny would give that group's users tools an administrator withheld.
    /// </remarks>
    private async Task<string> KeyUserGroupPermissionsByTargetGroupAsync(string configJson, string? agentAlias)
    {
        JsonObject? config = JsonNode.Parse(configJson)?.AsObject();
        if (config?[UserGroupPermissionsProperty] is not JsonObject permissions)
        {
            return configJson;
        }

        var rekeyed = new JsonObject();
        foreach ((string key, JsonNode? value) in permissions.ToList())
        {
            permissions.Remove(key);
            var userGroup = Guid.TryParse(key, out Guid userGroupKey)
                ? await _userGroupService.GetAsync(userGroupKey)
                : await _userGroupService.GetAsync(key);

            if (userGroup is not null)
            {
                rekeyed[userGroup.Key.ToString()] = value;
                continue;
            }

            if (HasDenies(value))
            {
                throw new InvalidOperationException(
                    $"Agent '{agentAlias}' denies tools for user group '{key}', which doesn't exist in this environment. "
                    + "Create the user group (with the same alias), then deploy the agent again.");
            }
        }

        config[UserGroupPermissionsProperty] = rekeyed;
        return config.ToJsonString();
    }

    private static bool HasDenies(JsonNode? permissions)
    {
        var entry = permissions?.Deserialize<AIAgentUserGroupPermissions>(JsonOptions);
        return entry is not null && (entry.DeniedToolIds.Count > 0 || entry.DeniedToolScopeIds.Count > 0);
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
