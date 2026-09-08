using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.Cms.Core.Events;
using Xunit;
using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

public class AIAgentServiceExecutionTests
{
    private static readonly Guid TestAgentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task RunAgentAsync_ForwardsAdditionalPropertiesToAgentFactory()
    {
        // Arrange
        var agent = CreateAgent(TestAgentId);
        var repositoryMock = new Mock<IAIAgentRepository>();
        repositoryMock
            .Setup(x => x.GetByIdAsync(TestAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        IReadOnlyDictionary<string, object?>? capturedAdditionalProperties = null;
        var agentFactoryMock = new Mock<IAIAgentFactory>();
        agentFactoryMock
            .Setup(x => x.CreateAgentAsync(
                agent,
                It.IsAny<IEnumerable<AIRequestContextItem>?>(),
                It.IsAny<IEnumerable<AITool>?>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<AIApprovalPolicy>(),
                It.IsAny<CancellationToken>()))
            .Callback<AIAgent, IEnumerable<AIRequestContextItem>?, IEnumerable<AITool>?, IReadOnlyDictionary<string, object?>?, AIApprovalPolicy, CancellationToken>(
                (_, _, _, properties, _, _) => capturedAdditionalProperties = properties)
            .ReturnsAsync(CreateRespondingAgent());

        var eventAggregatorMock = new Mock<IEventAggregator>();
        eventAggregatorMock
            .Setup(x => x.PublishAsync(It.IsAny<AIAgentExecutingNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        eventAggregatorMock
            .Setup(x => x.PublishAsync(It.IsAny<AIAgentExecutedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repositoryMock.Object, agentFactoryMock.Object, eventAggregatorMock.Object);
        var additionalProperties = new Dictionary<string, object?>
        {
            ["key-a"] = "value-a",
            ["key-b"] = 42,
        };

        // Act
        await service.RunAgentAsync(
            TestAgentId,
            [new ChatMessage(ChatRole.User, "Hello")],
            new AIAgentExecutionOptions { AdditionalProperties = additionalProperties },
            CancellationToken.None);

        // Assert
        capturedAdditionalProperties.ShouldNotBeNull();
        capturedAdditionalProperties!["key-a"].ShouldBe("value-a");
        capturedAdditionalProperties["key-b"].ShouldBe(42);
    }

    private static AIAgentService CreateService(
        IAIAgentRepository repository,
        IAIAgentFactory agentFactory,
        IEventAggregator eventAggregator,
        IAGUIStreamingService? streamingService = null,
        IAGUIContextConverter? contextConverter = null,
        IAGUIMessageConverter? messageConverter = null)
        => new(
            repository,
            null!, // IAIEntityVersionService
            agentFactory,
            streamingService!, // IAGUIStreamingService
            contextConverter!, // IAGUIContextConverter
            messageConverter!, // IAGUIMessageConverter
            new AIToolCollection(() => []),
            null!, // IAIProfileService
            null!, // IAIGuardrailService
            null!, // IAIContextService
            null!, // IAIChatClientFactory
            null!, // AIAgentScopeValidator
            null!, // AIAgentSurfaceCollection
            eventAggregator,
            null); // IBackOfficeSecurityAccessor

    [Fact]
    public async Task StreamAgentAGUIAsync_ForwardsOptionsAdditionalPropertiesToAgentFactory()
    {
        // Arrange
        var agent = CreateAgent(TestAgentId);
        var repositoryMock = new Mock<IAIAgentRepository>();
        repositoryMock
            .Setup(x => x.GetByIdAsync(TestAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        IReadOnlyDictionary<string, object?>? capturedAdditionalProperties = null;
        var agentFactoryMock = new Mock<IAIAgentFactory>();
        agentFactoryMock
            .Setup(x => x.CreateAgentAsync(
                agent,
                It.IsAny<IEnumerable<AIRequestContextItem>?>(),
                It.IsAny<IEnumerable<AITool>?>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<AIApprovalPolicy>(),
                It.IsAny<CancellationToken>()))
            .Callback<AIAgent, IEnumerable<AIRequestContextItem>?, IEnumerable<AITool>?, IReadOnlyDictionary<string, object?>?, AIApprovalPolicy, CancellationToken>(
                (_, _, _, properties, _, _) => capturedAdditionalProperties = properties)
            .ReturnsAsync(CreateRespondingAgent());

        var messageConverterMock = new Mock<IAGUIMessageConverter>();
        messageConverterMock
            .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>()))
            .Returns([new ChatMessage(ChatRole.User, "Hello")]);

        var contextConverterMock = new Mock<IAGUIContextConverter>();
        contextConverterMock
            .Setup(x => x.ConvertToRequestContextItems(It.IsAny<IEnumerable<AGUIContextItem>?>()))
            .Returns([]);

        var streamingServiceMock = new Mock<IAGUIStreamingService>();
        streamingServiceMock
            .Setup(x => x.StreamAgentAsync(
                It.IsAny<MsAIAgent>(),
                It.IsAny<AGUIRunRequest>(),
                It.IsAny<IEnumerable<AITool>?>(),
                It.IsAny<AgentSession?>(),
                It.IsAny<IReadOnlyDictionary<string, ToolApprovalRequestContent>?>(),
                It.IsAny<IReadOnlyList<ToolApprovalRequestContent>?>(),
                It.IsAny<CancellationToken>()))
            .Returns(EmptyEvents());

        var eventAggregatorMock = new Mock<IEventAggregator>();
        eventAggregatorMock
            .Setup(x => x.PublishAsync(It.IsAny<AIAgentExecutingNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        eventAggregatorMock
            .Setup(x => x.PublishAsync(It.IsAny<AIAgentExecutedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(
            repositoryMock.Object,
            agentFactoryMock.Object,
            eventAggregatorMock.Object,
            streamingServiceMock.Object,
            contextConverterMock.Object,
            messageConverterMock.Object);

        var request = new AGUIRunRequest { RunId = "run-1", ThreadId = "thread-1", Messages = [] };
        var options = new AIAgentExecutionOptions
        {
            AdditionalProperties = new Dictionary<string, object?> { ["project-key"] = "project-value" },
        };

        // Act — enumerate the stream to drive execution through PrepareAgentExecutionAsync.
        await foreach (var _ in service.StreamAgentAGUIAsync(TestAgentId, request, frontendTools: null, options, CancellationToken.None))
        {
        }

        // Assert — the caller's options.AdditionalProperties reach the factory (previously dropped on
        // this path), alongside the AG-UI-specific RunId/ThreadId keys.
        capturedAdditionalProperties.ShouldNotBeNull();
        capturedAdditionalProperties!["project-key"].ShouldBe("project-value");
        capturedAdditionalProperties[Constants.ContextKeys.RunId].ShouldBe("run-1");
        capturedAdditionalProperties[Constants.ContextKeys.ThreadId].ShouldBe("thread-1");
    }

    [Fact]
    public async Task RunAgentAsync_WithConversationHistory_BindsSessionAndSavesState()
    {
        // Arrange
        var agent = CreateAgent(TestAgentId);
        var fixture = CreatePersistedConversationFixture(agent);

        // Act
        var response = await fixture.Service.RunAgentAsync(
            TestAgentId,
            [new ChatMessage(ChatRole.User, "Hello")],
            new AIAgentExecutionOptions { ConversationHistory = fixture.HistoryBinding },
            CancellationToken.None);

        // Assert — BindSession must be invoked with a real session before the run, or the attached
        // ChatHistoryProvider never learns which conversation to persist to and every message is
        // silently dropped (the bug: session was always run as null on this path).
        fixture.BoundSession.ShouldNotBeNull();
        fixture.SaveStateCalled.ShouldBeTrue();
        // The bound ChatHistoryProvider itself must reach the factory too — a broken forwarding path
        // (e.g. accidentally passing null) would still leave BoundSession/SaveStateCalled true above,
        // since those are driven independently by this method's own session-creation code.
        fixture.CapturedProvider.ShouldBeSameAs(fixture.Provider);
        response.Text.ShouldBe("ok");
    }

    [Fact]
    public async Task StreamAgentAsync_WithConversationHistory_BindsSessionAndSavesState()
    {
        // Arrange
        var agent = CreateAgent(TestAgentId);
        var fixture = CreatePersistedConversationFixture(agent);

        // Act
        var updates = new List<AgentResponseUpdate>();
        await foreach (var update in fixture.Service.StreamAgentAsync(
            TestAgentId,
            [new ChatMessage(ChatRole.User, "Hello")],
            new AIAgentExecutionOptions { ConversationHistory = fixture.HistoryBinding },
            CancellationToken.None))
        {
            updates.Add(update);
        }

        // Assert — same session-binding requirement as the non-streaming path above, plus proof the
        // stream actually produced the agent's output (previously asserted nothing about it, so a
        // regression that silently yielded zero updates would have stayed green here).
        fixture.BoundSession.ShouldNotBeNull();
        fixture.SaveStateCalled.ShouldBeTrue();
        fixture.CapturedProvider.ShouldBeSameAs(fixture.Provider);
        string.Concat(updates.Select(u => u.Text)).ShouldBe("ok");
    }

    /// <summary>
    /// Shared arrange for the persisted-conversation tests above: a repository/agent-factory/event-
    /// aggregator wiring identical to <see cref="CreateService"/>'s callers, plus an
    /// <see cref="AIConversationHistoryBinding"/> whose BindSession/SaveSessionState/Provider forwarding
    /// is captured for assertions.
    /// </summary>
    private static PersistedConversationFixture CreatePersistedConversationFixture(AIAgent agent)
    {
        var fixture = new PersistedConversationFixture { Provider = new NoOpChatHistoryProvider() };

        var repositoryMock = new Mock<IAIAgentRepository>();
        repositoryMock
            .Setup(x => x.GetByIdAsync(agent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        var agentFactoryMock = new Mock<IAIAgentFactory>();
        agentFactoryMock
            .Setup(x => x.CreateAgentAsync(
                agent,
                It.IsAny<ChatHistoryProvider?>(),
                It.IsAny<IEnumerable<AIRequestContextItem>?>(),
                It.IsAny<IEnumerable<AITool>?>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<AIApprovalPolicy>(),
                It.IsAny<CancellationToken>()))
            .Callback<AIAgent, ChatHistoryProvider?, IEnumerable<AIRequestContextItem>?, IEnumerable<AITool>?, IReadOnlyDictionary<string, object?>?, AIApprovalPolicy, CancellationToken>(
                (_, boundProvider, _, _, _, _, _) => fixture.CapturedProvider = boundProvider)
            .ReturnsAsync(CreateRespondingAgent());

        var eventAggregatorMock = new Mock<IEventAggregator>();
        eventAggregatorMock
            .Setup(x => x.PublishAsync(It.IsAny<AIAgentExecutingNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        eventAggregatorMock
            .Setup(x => x.PublishAsync(It.IsAny<AIAgentExecutedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.Service = CreateService(repositoryMock.Object, agentFactoryMock.Object, eventAggregatorMock.Object);
        fixture.HistoryBinding = new AIConversationHistoryBinding(
            Provider: fixture.Provider,
            ConversationId: Guid.NewGuid(),
            BindSession: session => fixture.BoundSession = session)
        {
            SaveSessionState = (_, _) =>
            {
                fixture.SaveStateCalled = true;
                return ValueTask.CompletedTask;
            },
        };

        return fixture;
    }

    private sealed class PersistedConversationFixture
    {
        public AIAgentService Service { get; set; } = null!;
        public AIConversationHistoryBinding HistoryBinding { get; set; } = null!;
        public ChatHistoryProvider Provider { get; set; } = null!;
        public AgentSession? BoundSession { get; set; }
        public bool SaveStateCalled { get; set; }
        public ChatHistoryProvider? CapturedProvider { get; set; }
    }

    /// <summary>Minimal <see cref="ChatHistoryProvider"/> stand-in — only its identity matters here.</summary>
    private sealed class NoOpChatHistoryProvider : ChatHistoryProvider
    {
        protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
            InvokingContext context, CancellationToken cancellationToken = default)
            => new(Array.Empty<ChatMessage>());

        protected override ValueTask StoreChatHistoryAsync(
            InvokedContext context, CancellationToken cancellationToken = default)
            => default;
    }

    private static async IAsyncEnumerable<IAGUIEvent> EmptyEvents()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static AIAgent CreateAgent(Guid id)
        => new()
        {
            Id = id,
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = AIAgentType.Standard,
            Config = new AIStandardAgentConfig
            {
                AllowedToolIds = [],
                AllowedToolScopeIds = [],
            },
            IsActive = true,
        };

    private static MsAIAgent CreateRespondingAgent()
    {
        var chatClientMock = new Mock<IChatClient>();
        chatClientMock
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        chatClientMock
            .Setup(x => x.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(StreamingUpdates());

        return new ChatClientAgent(chatClientMock.Object);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamingUpdates()
    {
        await Task.CompletedTask;
        yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
    }
}
