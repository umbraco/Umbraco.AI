using Microsoft.Extensions.AI;
using Moq;
using Shouldly;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Core.Chat;
using Umbraco.Ai.Agent.Core.Context;
using Xunit;

namespace Umbraco.Ai.Agent.Tests.Unit.Chat;

public class AgentBoundChatClientTests
{
    private readonly Mock<IChatClient> _mockInnerClient;
    private readonly AiAgent _testAgent;

    public AgentBoundChatClientTests()
    {
        _mockInnerClient = new Mock<IChatClient>();
        _testAgent = new AiAgent
        {
            Id = Guid.NewGuid(),
            Alias = "test-agent",
            Name = "Test Agent",
            Description = "A test agent",
            ProfileId = Guid.NewGuid(),
            Instructions = "You are a helpful test assistant."
        };
    }

    [Fact]
    public async Task GetResponseAsync_InjectsAgentIdIntoOptions()
    {
        // Arrange
        ChatOptions? capturedOptions = null;
        _mockInnerClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((_, options, _) =>
            {
                capturedOptions = options;
            })
            .ReturnsAsync(new ChatResponse([]));

        var client = new AgentBoundChatClient(_mockInnerClient.Object, _testAgent);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        // Act
        await client.GetResponseAsync(messages);

        // Assert
        capturedOptions.ShouldNotBeNull();
        capturedOptions.AdditionalProperties.ShouldNotBeNull();
        capturedOptions.AdditionalProperties.ShouldContainKey(AgentContextResolver.AgentIdKey);
        capturedOptions.AdditionalProperties[AgentContextResolver.AgentIdKey].ShouldBe(_testAgent.Id);
    }

    [Fact]
    public async Task GetResponseAsync_PrependsInstructionsToSystemMessage()
    {
        // Arrange
        IEnumerable<ChatMessage>? capturedMessages = null;
        _mockInnerClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((messages, _, _) =>
            {
                capturedMessages = messages;
            })
            .ReturnsAsync(new ChatResponse([]));

        var client = new AgentBoundChatClient(_mockInnerClient.Object, _testAgent);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        // Act
        await client.GetResponseAsync(messages);

        // Assert
        capturedMessages.ShouldNotBeNull();
        var messageList = capturedMessages.ToList();
        messageList.Count.ShouldBe(2);
        messageList[0].Role.ShouldBe(ChatRole.System);
        messageList[0].Text.ShouldContain(_testAgent.Instructions);
        messageList[1].Role.ShouldBe(ChatRole.User);
    }

    [Fact]
    public async Task GetResponseAsync_CombinesInstructionsWithExistingSystemMessage()
    {
        // Arrange
        var existingSystemContent = "Existing system content";
        IEnumerable<ChatMessage>? capturedMessages = null;
        _mockInnerClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((messages, _, _) =>
            {
                capturedMessages = messages;
            })
            .ReturnsAsync(new ChatResponse([]));

        var client = new AgentBoundChatClient(_mockInnerClient.Object, _testAgent);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, existingSystemContent),
            new(ChatRole.User, "Hello")
        };

        // Act
        await client.GetResponseAsync(messages);

        // Assert
        capturedMessages.ShouldNotBeNull();
        var messageList = capturedMessages.ToList();
        messageList.Count.ShouldBe(2);
        messageList[0].Role.ShouldBe(ChatRole.System);
        messageList[0].Text.ShouldContain(_testAgent.Instructions);
        messageList[0].Text.ShouldContain(existingSystemContent);
        messageList[1].Role.ShouldBe(ChatRole.User);
    }

    [Fact]
    public async Task GetResponseAsync_NoInstructions_DoesNotAddSystemMessage()
    {
        // Arrange
        var agentWithoutInstructions = new AiAgent
        {
            Id = Guid.NewGuid(),
            Alias = "test-agent",
            Name = "Test Agent",
            ProfileId = Guid.NewGuid(),
            Instructions = null
        };

        IEnumerable<ChatMessage>? capturedMessages = null;
        _mockInnerClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((messages, _, _) =>
            {
                capturedMessages = messages;
            })
            .ReturnsAsync(new ChatResponse([]));

        var client = new AgentBoundChatClient(_mockInnerClient.Object, agentWithoutInstructions);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        // Act
        await client.GetResponseAsync(messages);

        // Assert
        capturedMessages.ShouldNotBeNull();
        var messageList = capturedMessages.ToList();
        messageList.Count.ShouldBe(1);
        messageList[0].Role.ShouldBe(ChatRole.User);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_InjectsAgentIdIntoOptions()
    {
        // Arrange
        ChatOptions? capturedOptions = null;
        _mockInnerClient
            .Setup(x => x.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((_, options, _) =>
            {
                capturedOptions = options;
            })
            .Returns(AsyncEnumerable.Empty<ChatResponseUpdate>());

        var client = new AgentBoundChatClient(_mockInnerClient.Object, _testAgent);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        // Act
        await foreach (var _ in client.GetStreamingResponseAsync(messages))
        {
            // Consume the stream
        }

        // Assert
        capturedOptions.ShouldNotBeNull();
        capturedOptions.AdditionalProperties.ShouldNotBeNull();
        capturedOptions.AdditionalProperties.ShouldContainKey(AgentContextResolver.AgentIdKey);
        capturedOptions.AdditionalProperties[AgentContextResolver.AgentIdKey].ShouldBe(_testAgent.Id);
    }

    [Fact]
    public void GetService_ReturnsSelf_WhenAgentBoundChatClientRequested()
    {
        // Arrange
        var client = new AgentBoundChatClient(_mockInnerClient.Object, _testAgent);

        // Act
        var service = client.GetService(typeof(AgentBoundChatClient));

        // Assert
        service.ShouldBe(client);
    }

    [Fact]
    public void GetService_DelegatesToInnerClient_ForOtherServices()
    {
        // Arrange
        var expectedService = new object();
        _mockInnerClient
            .Setup(x => x.GetService(typeof(string), null))
            .Returns(expectedService);

        var client = new AgentBoundChatClient(_mockInnerClient.Object, _testAgent);

        // Act
        var service = client.GetService(typeof(string));

        // Assert
        service.ShouldBe(expectedService);
    }

    [Fact]
    public void Dispose_DisposesInnerClient()
    {
        // Arrange
        var client = new AgentBoundChatClient(_mockInnerClient.Object, _testAgent);

        // Act
        client.Dispose();

        // Assert
        _mockInnerClient.Verify(x => x.Dispose(), Times.Once);
    }
}
