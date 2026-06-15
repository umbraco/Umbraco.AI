using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Chat;
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
                It.IsAny<CancellationToken>()))
            .Callback<AIAgent, IEnumerable<AIRequestContextItem>?, IEnumerable<AITool>?, IReadOnlyDictionary<string, object?>?, CancellationToken>(
                (_, _, _, properties, _) => capturedAdditionalProperties = properties)
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
        IEventAggregator eventAggregator)
        => new(
            repository,
            null!, // IAIEntityVersionService
            agentFactory,
            null!, // IAGUIStreamingService
            null!, // IAGUIContextConverter
            null!, // IAGUIMessageConverter
            new AIToolCollection(() => []),
            null!, // IAIProfileService
            null!, // IAIGuardrailService
            null!, // IAIContextService
            null!, // IAIChatClientFactory
            null!, // AIAgentScopeValidator
            null!, // AIAgentSurfaceCollection
            eventAggregator,
            null); // IBackOfficeSecurityAccessor

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

        return new ChatClientAgent(chatClientMock.Object);
    }
}
