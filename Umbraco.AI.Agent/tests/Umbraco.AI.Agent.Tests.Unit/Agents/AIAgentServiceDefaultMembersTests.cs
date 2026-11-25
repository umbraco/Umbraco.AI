// T21 (public IAIAgentService additions must not break external implementers)

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.InlineAgents;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;

using Xunit;

using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// An implementer written against the old IAIAgentService surface still compiles, and the members added since
/// fail closed.
/// </summary>
public class AIAgentServiceDefaultMembersTests
{
    private static readonly Guid Limited = Guid.NewGuid();

    // Knows the old surface only: one agent that the "current user" can't use, so the filtering getters hide it.
    private readonly OldSurfaceAgentService _old = new();

    private IAIAgentService Service => _old;

    [Fact]
    public async Task GetAgentForUseAsync_OnALimitedAgent_ReadsAsNotFound()
    {
        // Given an implementer of the old surface whose filtering getter hides the agent
        // When  the new for-use getter is called
        var agent = await Service.GetAgentForUseAsync(Limited);

        // Then  it asked the filtering getter and returned null, not the agent
        agent.ShouldBeNull();
        _old.GetAgentCalls.ShouldBe(1);
    }

    [Fact]
    public async Task GetAgentForUseByAliasAsync_OnALimitedAgent_ReadsAsNotFound()
    {
        // Given an implementer of the old surface whose filtering getter hides the agent
        // When  the new for-use alias getter is called
        var agent = await Service.GetAgentForUseByAliasAsync("limited");

        // Then  it asked the filtering getter and returned null, not the agent
        agent.ShouldBeNull();
        _old.GetAgentByAliasCalls.ShouldBe(1);
    }

    [Fact]
    public async Task GetAgentAsync_WithIncludeRestricted_IgnoresTheFlag()
    {
        // Given an implementer of the old surface
        // When  the includeRestricted overloads are called with true
        var byId = await Service.GetAgentAsync(Limited, includeRestricted: true);
        var byAlias = await Service.GetAgentByAliasAsync("limited", includeRestricted: true);
        var paged = await Service.GetAgentsPagedAsync(0, 10, null, null, null, null, null, includeRestricted: true);

        // Then  the restricted agent is still hidden
        byId.ShouldBeNull();
        byAlias.ShouldBeNull();
        paged.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExplicitUserGroupOverloads_Throw_NotSupported()
    {
        // Given an implementer of the old surface
        IReadOnlyCollection<Guid> groups = [Guid.NewGuid()];

        // When  an overload that takes explicit user groups is called
        // Then  it throws, rather than answering for groups it can't check
        await Should.ThrowAsync<NotSupportedException>(() => Service.GetAgentAsync(Limited, groups));
        await Should.ThrowAsync<NotSupportedException>(() => Service.GetAgentByAliasAsync("limited", groups));
        await Should.ThrowAsync<NotSupportedException>(
            () => Service.GetAgentsPagedAsync(0, 10, null, null, null, null, null, groups));
    }

    // Implements only the members IAIAgentService had before granular permissions.
#pragma warning disable CS0618 // SelectAgentForPromptAsync is obsolete but still part of the surface
    private sealed class OldSurfaceAgentService : IAIAgentService
    {
        public int GetAgentCalls { get; private set; }

        public int GetAgentByAliasCalls { get; private set; }

        public Task<AIAgent?> GetAgentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetAgentCalls++;
            return Task.FromResult<AIAgent?>(null);
        }

        public Task<AIAgent?> GetAgentByAliasAsync(string alias, CancellationToken cancellationToken = default)
        {
            GetAgentByAliasCalls++;
            return Task.FromResult<AIAgent?>(null);
        }

        public Task<IEnumerable<AIAgent>> GetAgentsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<AIAgent>>([]);

        public Task<(IEnumerable<AIAgent> Items, int Total)> GetAgentsPagedAsync(
            int skip, int take, string? filter = null, Guid? profileId = null, string? surfaceId = null,
            bool? isActive = null, AIAgentType? agentType = null, CancellationToken cancellationToken = default)
            => Task.FromResult<(IEnumerable<AIAgent>, int)>(([], 0));

        public Task<IEnumerable<AIAgent>> GetAgentsBySurfaceAsync(string surfaceId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<AIAgent>>([]);

        public Task<AIAgent> SaveAgentAsync(AIAgent agent, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> AgentAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> AgentsExistWithProfileAsync(Guid profileId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<string>> GetAllowedToolIdsAsync(AIAgent agent, IEnumerable<Guid>? userGroupIds = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> IsToolAllowedAsync(AIAgent agent, string toolId, IEnumerable<Guid>? userGroupIds = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<AIAgent?> SelectAgentForPromptAsync(string userPrompt, string surfaceId, AgentAvailabilityContext context, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<AgentResponse> RunAgentAsync(Guid agentId, IEnumerable<ChatMessage> messages, AIAgentExecutionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<AgentResponse> RunAgentAsync(string agentAlias, IEnumerable<ChatMessage> messages, AIAgentExecutionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public IAsyncEnumerable<AgentResponseUpdate> StreamAgentAsync(Guid agentId, IEnumerable<ChatMessage> messages, AIAgentExecutionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public IAsyncEnumerable<AgentResponseUpdate> StreamAgentAsync(string agentAlias, IEnumerable<ChatMessage> messages, AIAgentExecutionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public IAsyncEnumerable<IAGUIEvent> StreamAgentAGUIAsync(Guid agentId, AGUIRunRequest request, IEnumerable<AIFrontendTool>? frontendTools, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public IAsyncEnumerable<IAGUIEvent> StreamAgentAGUIAsync(Guid agentId, AGUIRunRequest request, IEnumerable<AIFrontendTool>? frontendTools, AIAgentExecutionOptions options, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<MsAIAgent> CreateInlineAgentAsync(Action<AIInlineAgentBuilder> configure, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<AgentResponse> RunAgentAsync(Action<AIInlineAgentBuilder> configure, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public IAsyncEnumerable<AgentResponseUpdate> StreamAgentAsync(Action<AIInlineAgentBuilder> configure, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
#pragma warning restore CS0618
}
