using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;
using AgentConstants = Umbraco.AI.Agent.Core.Constants;
using CoreConstants = Umbraco.AI.Core.Constants;
using UmbracoAIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

/// <summary>
/// Tests for <see cref="ScopedAIAgent"/>.
/// </summary>
public class ScopedAIAgentTests
{
    [Fact]
    public void StageSystemMessageParts_WithParts_StagesThePendingSystemMessage()
    {
        // Arrange
        var context = new AIRuntimeContext([]);
        context.SystemMessageParts.Add("## Current Entity Context");
        context.SystemMessageParts.Add("- Page: About Us");

        // Act
        ScopedAIAgent.StageSystemMessageParts(context);

        // Assert
        context.TryGetValue<string>(AgentConstants.ContextKeys.PendingSystemMessage, out var staged).ShouldBeTrue();
        staged.ShouldBe("## Current Entity Context\n\n- Page: About Us");
    }

    [Fact]
    public void StageSystemMessageParts_WithNoParts_StagesNothing()
    {
        // Arrange
        var context = new AIRuntimeContext([]);

        // Act
        ScopedAIAgent.StageSystemMessageParts(context);

        // Assert
        context.TryGetValue<string>(AgentConstants.ContextKeys.PendingSystemMessage, out _).ShouldBeFalse();
    }

    [Fact]
    public async Task RunStreamingAsync_EveryUpdateSeesTheAgentsContext()
    {
        // The agent's scope is created inside its streaming iterator, so it must stay current for every step.
        var scopeProvider = new AIRuntimeContextScopeProvider();
        var probe = new ContextProbeChatClient(scopeProvider, updates: 3);
        var agent = CreateAgent("content-assistant", probe, scopeProvider);

        await foreach (var _ in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
            scopeProvider.Context.ShouldBeNull();
        }

        probe.Seen.ShouldBe(["content-assistant", "content-assistant", "content-assistant"]);
        scopeProvider.Context.ShouldBeNull();
    }

    [Fact]
    public async Task RunStreamingAsync_TwoAgentsReadInTurn_EachUpdateSeesItsOwnAgent()
    {
        var scopeProvider = new AIRuntimeContextScopeProvider();
        var probeA = new ContextProbeChatClient(scopeProvider, updates: 3);
        var probeB = new ContextProbeChatClient(scopeProvider, updates: 3);

        await using var a = CreateAgent("agent-a", probeA, scopeProvider)
            .RunStreamingAsync([new ChatMessage(ChatRole.User, "a")]).GetAsyncEnumerator();
        await using var b = CreateAgent("agent-b", probeB, scopeProvider)
            .RunStreamingAsync([new ChatMessage(ChatRole.User, "b")]).GetAsyncEnumerator();
        while (await a.MoveNextAsync() & await b.MoveNextAsync())
        {
        }

        probeA.Seen.ShouldBe(["agent-a", "agent-a", "agent-a"]);
        probeB.Seen.ShouldBe(["agent-b", "agent-b", "agent-b"]);
    }

    [Fact]
    public async Task RunStreamingAsync_AgentsRunTogether_EachSeesOnlyItsOwnAgent()
    {
        var scopeProvider = new AIRuntimeContextScopeProvider();
        var probes = Enumerable.Range(0, 20).Select(_ => new ContextProbeChatClient(scopeProvider, updates: 3)).ToList();

        await Task.WhenAll(probes.Select((probe, i) => Task.Run(async () =>
        {
            await foreach (var _ in CreateAgent($"agent-{i}", probe, scopeProvider)
                               .RunStreamingAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
            }
        })));

        for (var i = 0; i < probes.Count; i++)
        {
            probes[i].Seen.ShouldBe([$"agent-{i}", $"agent-{i}", $"agent-{i}"]);
        }
    }

    private static ScopedAIAgent CreateAgent(string alias, IChatClient chatClient, AIRuntimeContextScopeProvider scopeProvider)
        => new(
            new ChatClientAgent(chatClient, new ChatClientAgentOptions { Name = alias }),
            new UmbracoAIAgent { Alias = alias, Name = alias },
            [],
            [],
            null,
            scopeProvider,
            new AIRuntimeContextContributorCollection(() => []));

    /// <summary>Streams updates, recording the feature alias of the current runtime context at each one.</summary>
    private sealed class ContextProbeChatClient(IAIRuntimeContextAccessor accessor, int updates) : IChatClient
    {
        public List<string?> Seen { get; } = [];

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < updates; i++)
            {
                await Task.Yield();
                Seen.Add(accessor.Context?.GetValue<string>(CoreConstants.ContextKeys.FeatureAlias));
                yield return new ChatResponseUpdate(ChatRole.Assistant, $"{i}");
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
