using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Deploy.Agent.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.AI.Deploy.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Core;

namespace Umbraco.AI.Deploy.Agent.Connectors.ServiceConnectors;

[UdiDefinition(UmbracoAIAgentConstants.UdiEntityType.Agent, UdiType.GuidUdi)]
public class UmbracoAIAgentServiceConnector(
    IAIAgentService agentService,
    IAIProfileService profileService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIProfileDependentEntityServiceConnectorBase<AIAgentArtifact, AIAgent>(
        profileService,
        settingsAccessor)
{
    private readonly IAIAgentService _agentService = agentService;

    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];
    protected override string OpenUdiName => "All Umbraco AI Agents";
    public override string UdiEntityType => UmbracoAIAgentConstants.UdiEntityType.Agent;

    public override Task<AIAgent?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => _agentService.GetAgentAsync(id, cancellationToken);

    public override async IAsyncEnumerable<AIAgent> GetEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var agents = await _agentService.GetAgentsAsync(cancellationToken);
        foreach (var agent in agents)
        {
            yield return agent;
        }
    }

    public override string GetEntityName(AIAgent entity)
        => entity.Name;

    public override Task<AIAgentArtifact?> GetArtifactAsync(
        GuidUdi? udi,
        AIAgent? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null) return Task.FromResult<AIAgentArtifact?>(null);

        var dependencies = new ArtifactDependencyCollection();

        // Use base class helper for optional profile dependency
        var profileUdi = AddProfileDependency(entity.ProfileId, dependencies);

        // Add user group dependencies (ensure user groups exist in target environment)
        if (entity.UserGroupPermissions.Count > 0)
        {
            foreach (var userGroupId in entity.UserGroupPermissions.Keys)
            {
                var userGroupUdi = new GuidUdi("user-group", userGroupId);
                dependencies.Add(new ArtifactDependency(userGroupUdi, false, ArtifactDependencyMode.Exist));
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
            UserGroupPermissions = entity.UserGroupPermissions.Count > 0
                ? JsonSerializer.SerializeToElement(entity.UserGroupPermissions)
                : null,
            Instructions = entity.Instructions,
            IsActive = entity.IsActive,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId,
            Version = entity.Version
        };

        return Task.FromResult<AIAgentArtifact?>(artifact);
    }

    public override async Task ProcessAsync(
        ArtifactDeployState<AIAgentArtifact, AIAgent> state,
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
        ArtifactDeployState<AIAgentArtifact, AIAgent> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        // Deserialize Scope from JsonElement
        AIAgentScope? scope = null;
        if (artifact.Scope.HasValue)
        {
            scope = JsonSerializer.Deserialize<AIAgentScope>(artifact.Scope.Value);
        }

        // Deserialize UserGroupPermissions from JsonElement
        Dictionary<Guid, AIAgentUserGroupPermissions>? userGroupPermissions = null;
        if (artifact.UserGroupPermissions.HasValue)
        {
            userGroupPermissions = JsonSerializer.Deserialize<Dictionary<Guid, AIAgentUserGroupPermissions>>(artifact.UserGroupPermissions.Value);
        }

        if (state.Entity == null)
        {
            // Create new agent (ProfileId will be resolved in Pass 4)
            var agent = new AIAgent
            {
                Alias = artifact.Alias,
                Name = artifact.Name,
                Description = artifact.Description,
                ProfileId = null, // Will be resolved in Pass 4
                ContextIds = artifact.ContextIds.ToList(),
                SurfaceIds = artifact.SurfaceIds.ToList(),
                Scope = scope,
                AllowedToolIds = artifact.AllowedToolIds.ToList(),
                AllowedToolScopeIds = artifact.AllowedToolScopeIds.ToList(),
                UserGroupPermissions = userGroupPermissions ?? new Dictionary<Guid, AIAgentUserGroupPermissions>(),
                Instructions = artifact.Instructions,
                IsActive = artifact.IsActive,
                CreatedByUserId = artifact.CreatedByUserId,
                ModifiedByUserId = artifact.ModifiedByUserId
            };

            state.Entity = await _agentService.SaveAgentAsync(agent, cancellationToken);
        }
        else
        {
            // Update existing agent
            var agent = state.Entity;
            agent.Name = artifact.Name;
            agent.Description = artifact.Description;
            agent.ContextIds = artifact.ContextIds.ToList();
            agent.SurfaceIds = artifact.SurfaceIds.ToList();
            agent.Scope = scope;
            agent.AllowedToolIds = artifact.AllowedToolIds.ToList();
            agent.AllowedToolScopeIds = artifact.AllowedToolScopeIds.ToList();
            agent.UserGroupPermissions = userGroupPermissions ?? new Dictionary<Guid, AIAgentUserGroupPermissions>();
            agent.Instructions = artifact.Instructions;
            agent.IsActive = artifact.IsActive;
            agent.ModifiedByUserId = artifact.ModifiedByUserId;
            // ProfileId will be updated in Pass 4

            state.Entity = await _agentService.SaveAgentAsync(agent, cancellationToken);
        }
    }

    private async Task Pass4Async(
        ArtifactDeployState<AIAgentArtifact, AIAgent> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        // Use base class helper to resolve optional ProfileId from ProfileUdi
        var profileId = await ResolveProfileIdAsync(artifact.ProfileUdi, cancellationToken);

        // Update agent with resolved ProfileId
        var agent = state.Entity!;
        agent.ProfileId = profileId;

        state.Entity = await _agentService.SaveAgentAsync(agent, cancellationToken);
    }
}
