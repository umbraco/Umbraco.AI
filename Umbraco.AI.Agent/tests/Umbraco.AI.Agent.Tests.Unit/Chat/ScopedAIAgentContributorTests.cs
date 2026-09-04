using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;
using UmbracoAIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

/// <summary>
/// <see cref="ScopedAIAgent"/> is the entry point every Copilot chat turn goes through
/// (StreamAgentAGUIController → AIAgentService → AIAgentFactory → ScopedAIAgent). Contributors that
/// need async work — e.g. resolving and extracting text from the currently open media file — only
/// run that work if the scope is populated via
/// <see cref="AIRuntimeContextContributorCollection.PopulateAsync"/>. These tests pin that.
/// </summary>
public class ScopedAIAgentContributorTests
{
    /// <summary>
    /// A non-streaming run must populate the scope through <c>PopulateAsync</c>.
    /// </summary>
    [Fact]
    public async Task RunAsync_PopulatesScopeViaContributeAsync_NotContribute()
    {
        // Arrange
        var contributor = new AsyncAwareContributor();
        var (agent, chatClient) = CreateScopedAgent(contributor);

        // Act
        await agent.RunAsync([new ChatMessage(ChatRole.User, "hi")]);

        // Assert
        contributor.AsyncCalled.ShouldBeTrue();
        contributor.SyncCalled.ShouldBeFalse();

        // ...and the async contribution actually reached the model as a system message
        chatClient.LastMessages.ShouldNotBeNull();
        chatClient.LastMessages!
            .Any(m => m.Role == ChatRole.System && (m.Text ?? string.Empty).Contains(AsyncAwareContributor.Marker))
            .ShouldBeTrue();
    }

    /// <summary>
    /// A streaming run — the path Copilot chat uses — must populate the scope through
    /// <c>PopulateAsync</c> too.
    /// </summary>
    [Fact]
    public async Task RunStreamingAsync_PopulatesScopeViaContributeAsync_NotContribute()
    {
        // Arrange — streaming is the path Copilot chat actually uses
        var contributor = new AsyncAwareContributor();
        var (agent, chatClient) = CreateScopedAgent(contributor);

        // Act
        await foreach (var _ in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
            // drain
        }

        // Assert
        contributor.AsyncCalled.ShouldBeTrue();
        contributor.SyncCalled.ShouldBeFalse();
        chatClient.LastMessages.ShouldNotBeNull();
        chatClient.LastMessages!
            .Any(m => m.Role == ChatRole.System && (m.Text ?? string.Empty).Contains(AsyncAwareContributor.Marker))
            .ShouldBeTrue();
    }

    /// <summary>
    /// Making population async must not disturb the agent metadata written straight after it.
    /// </summary>
    [Fact]
    public async Task RunAsync_StillSetsAgentMetadataOnTheContext()
    {
        // Arrange — making population async must not disturb the metadata written after it
        var contributor = new AsyncAwareContributor();
        var definition = CreateDefinition();
        var scopeProvider = new TestScopeProvider();
        var agent = CreateScopedAgent(contributor, definition, scopeProvider, out _);

        // Act
        await agent.RunAsync([new ChatMessage(ChatRole.User, "hi")]);

        // Assert
        var context = scopeProvider.LastContext.ShouldNotBeNull();
        context.Data[Umbraco.AI.Agent.Core.Constants.ContextKeys.AgentId].ShouldBe(definition.Id);
        context.Data[Umbraco.AI.Agent.Core.Constants.ContextKeys.AgentAlias].ShouldBe(definition.Alias);
        context.Data[Umbraco.AI.Core.Constants.ContextKeys.FeatureType].ShouldBe("agent");
        context.Data[Umbraco.AI.Core.Constants.ContextKeys.FeatureId].ShouldBe(definition.Id);
        context.Data[Umbraco.AI.Core.Constants.ContextKeys.FeatureAlias].ShouldBe(definition.Alias);
    }

    private static (ScopedAIAgent Agent, RecordingChatClient ChatClient) CreateScopedAgent(
        IAIRuntimeContextContributor contributor)
    {
        var agent = CreateScopedAgent(contributor, CreateDefinition(), new TestScopeProvider(), out var chatClient);
        return (agent, chatClient);
    }

    private static ScopedAIAgent CreateScopedAgent(
        IAIRuntimeContextContributor contributor,
        UmbracoAIAgent definition,
        TestScopeProvider scopeProvider,
        out RecordingChatClient chatClient)
    {
        chatClient = new RecordingChatClient();
        var innerAgent = new ChatClientAgent(chatClient);

        return new ScopedAIAgent(
            innerAgent,
            definition,
            [],
            [],
            null,
            scopeProvider,
            new AIRuntimeContextContributorCollection(() => [contributor]));
    }

    private static UmbracoAIAgent CreateDefinition()
        => new()
        {
            Id = Guid.NewGuid(),
            Alias = "test-agent",
            Name = "Test Agent",
            ProfileId = Guid.NewGuid(),
            AgentType = AIAgentType.Standard,
            Config = new AIStandardAgentConfig
            {
                AllowedToolIds = [],
                AllowedToolScopeIds = [],
            },
            IsActive = true,
        };

    /// <summary>
    /// A contributor that overrides <c>ContributeAsync</c>, exactly like
    /// <c>SerializedEntityContributor</c> does. If the scope is populated through the sync
    /// <c>Populate</c> instead, <see cref="Contribute"/> runs and the async work never happens.
    /// </summary>
    private sealed class AsyncAwareContributor : IAIRuntimeContextContributor
    {
        public const string Marker = "async-contributed";

        public bool SyncCalled { get; private set; }

        public bool AsyncCalled { get; private set; }

        public void Contribute(AIRuntimeContext context) => SyncCalled = true;

        public Task ContributeAsync(AIRuntimeContext context, CancellationToken cancellationToken = default)
        {
            AsyncCalled = true;
            context.SystemMessageParts.Add(Marker);
            return Task.CompletedTask;
        }
    }

    private sealed class TestScopeProvider : IAIRuntimeContextScopeProvider
    {
        public AIRuntimeContext? LastContext { get; private set; }

        public IAIRuntimeContextScope CreateScope() => CreateScope([]);

        public IAIRuntimeContextScope CreateScope(IEnumerable<AIRequestContextItem> items)
        {
            var context = new AIRuntimeContext(items);
            LastContext = context;
            return new TestScope(context);
        }

        private sealed class TestScope : IAIRuntimeContextScope
        {
            public TestScope(AIRuntimeContext context) => Context = context;

            public AIRuntimeContext Context { get; }

            public AIRuntimeContext? ParentContext => null;

            public int Depth => 1;

            public void Dispose()
            {
            }
        }
    }

    private sealed class RecordingChatClient : IChatClient
    {
        public IReadOnlyList<ChatMessage>? LastMessages { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastMessages = messages.ToList();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastMessages = messages.ToList();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
            await Task.CompletedTask;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
