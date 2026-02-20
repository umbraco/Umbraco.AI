using System.Runtime.CompilerServices;
using System.Text.Json;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Agent.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Agent.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for deploying Umbraco AI Agents. Handles export and import of agents, including their dependencies on profiles. User group permissions are resolved by alias.
/// </summary>
[UdiDefinition(UmbracoAIAgentConstants.UdiEntityType.Agent, UdiType.GuidUdi)]
public class UmbracoAIAgentServiceConnector(
    IAIAgentService agentService,
    IAIProfileService profileService,
    IUserGroupService userGroupService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIProfileDependentEntityServiceConnectorBase<AIAgentArtifact, AIAgent>(
        profileService,
        settingsAccessor)
{
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
    public override async Task<AIAgentArtifact?> GetArtifactAsync(
        GuidUdi udi,
        AIAgent? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null) return null;

        var dependencies = new ArtifactDependencyCollection();

        // Use base class helper for optional profile dependency
        var profileUdi = AddProfileDependency(entity.ProfileId, dependencies);

        // Convert user group permissions from GUID-keyed to alias-keyed
        Dictionary<string, AIAgentUserGroupPermissions>? aliasKeyedPermissions = null;
        if (entity.UserGroupPermissions.Count > 0)
        {
            aliasKeyedPermissions = new Dictionary<string, AIAgentUserGroupPermissions>();
            foreach (var (userGroupId, permissions) in entity.UserGroupPermissions)
            {
                var userGroup = await userGroupService.GetAsync(userGroupId);
                if (userGroup != null)
                {
                    aliasKeyedPermissions[userGroup.Alias] = permissions;
                }
            }
        }

        var artifact = new AIAgentArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            ProfileUdi = profileUdi,
            ContextIds = entity.ContextIds.ToList(),
            SurfaceIds = entity.SurfaceIds.ToList(),
            Scope = entity.Scope != null ? JsonSerializer.SerializeToElement(entity.Scope) : null,
            AllowedToolIds = entity.AllowedToolIds.ToList(),
            AllowedToolScopeIds = entity.AllowedToolScopeIds.ToList(),
            UserGroupPermissions = aliasKeyedPermissions != null && aliasKeyedPermissions.Count > 0
                ? JsonSerializer.SerializeToElement(aliasKeyedPermissions)
                : null,
            Instructions = entity.Instructions,
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

        // Deserialize UserGroupPermissions and resolve aliases to GUIDs
        Dictionary<Guid, AIAgentUserGroupPermissions> userGroupPermissions = new();
        if (artifact.UserGroupPermissions.HasValue)
        {
            var aliasKeyedPermissions = artifact.UserGroupPermissions.Value
                .Deserialize<Dictionary<string, AIAgentUserGroupPermissions>>();

            if (aliasKeyedPermissions != null)
            {
                foreach (var (alias, permissions) in aliasKeyedPermissions)
                {
                    var userGroup = await userGroupService.GetAsync(alias);
                    if (userGroup != null)
                    {
                        userGroupPermissions[userGroup.Key] = permissions;
                    }
                }
            }
        }

        // Get or create agent entity
        var agent = state.Entity
            ?? new AIAgent
            {
                Id = artifact.Udi.Guid,
                Alias = artifact.Alias!,
                Name = artifact.Name
            };

        // Update properties from artifact
        agent.Alias = artifact.Alias!;
        agent.Name = artifact.Name;
        agent.Description = artifact.Description;
        agent.ProfileId = profileId;
        agent.ContextIds = artifact.ContextIds.ToList();
        agent.SurfaceIds = artifact.SurfaceIds.ToList();
        agent.Scope = scope;
        agent.AllowedToolIds = artifact.AllowedToolIds.ToList();
        agent.AllowedToolScopeIds = artifact.AllowedToolScopeIds.ToList();
        agent.UserGroupPermissions = userGroupPermissions;
        agent.Instructions = artifact.Instructions;
        agent.IsActive = artifact.IsActive;

        state.Entity = await agentService.SaveAgentAsync(agent, cancellationToken);
    }
}
