using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Services;

public class AIChatServiceTests
{
    private readonly Mock<IAIChatClientFactory> _clientFactoryMock;
    private readonly Mock<IAIProfileService> _profileServiceMock;
    private readonly Mock<IOptionsMonitor<AIOptions>> _optionsMock;
    private readonly AIChatService _service;

    public AIChatServiceTests()
    {
        _clientFactoryMock = new Mock<IAIChatClientFactory>();
        _profileServiceMock = new Mock<IAIProfileService>();
        _optionsMock = new Mock<IOptionsMonitor<AIOptions>>();
        _optionsMock.Setup(x => x.CurrentValue).Returns(new AIOptions
        {
            DefaultChatProfileAlias = "default-chat"
        });

        _service = new AIChatService(
            _clientFactoryMock.Object,
            _profileServiceMock.Object,
            _optionsMock.Object);
    }

    #region GetChatResponseAsync - Default profile

    [Fact]
    public async Task GetChatResponseAsync_WithDefaultProfile_UsesDefaultProfile()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var defaultProfile = new AIProfileBuilder()
            .WithAlias("default-chat")
            .WithCapability(AICapability.Chat)
            .WithModel("openai", "gpt-4")
            .Build();

        var fakeChatClient = new FakeChatClient("Hello! How can I help you?");

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(defaultProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        var response = await _service.GetChatResponseAsync(messages);

        // Assert
        response.ShouldNotBeNull();
        response.Text.ShouldBe("Hello! How can I help you?");
        _profileServiceMock.Verify(
            x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetChatResponseAsync - By profile ID

    [Fact]
    public async Task GetChatResponseAsync_WithProfileId_UsesSpecifiedProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithCapability(AICapability.Chat)
            .WithModel("openai", "gpt-4")
            .Build();

        var fakeChatClient = new FakeChatClient("Response from specific profile");

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        var response = await _service.GetChatResponseAsync(profileId, messages);

        // Assert
        response.ShouldNotBeNull();
        response.Text.ShouldBe("Response from specific profile");
        _profileServiceMock.Verify(
            x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetChatResponseAsync_WithNonExistentProfileId_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        // Act
        var act = () => _service.GetChatResponseAsync(profileId, messages);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain($"AI profile with ID '{profileId}' not found");
    }

    [Fact]
    public async Task GetChatResponseAsync_WithEmbeddingProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var embeddingProfile = new AIProfileBuilder()
            .WithId(profileId)
            .WithCapability(AICapability.Embedding)
            .WithName("Embedding Profile")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingProfile);

        // Act
        var act = () => _service.GetChatResponseAsync(profileId, messages);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("does not support chat capability");
    }

    #endregion

    #region GetChatResponseAsync - Options merging

    [Fact]
    public async Task GetChatResponseAsync_WithCallerOptions_MergesWithProfileDefaults()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AIProfileBuilder()
            .WithCapability(AICapability.Chat)
            .WithModel("openai", "gpt-4")
            .WithChatSettings(temperature: 0.7f, maxTokens: 1000)
            .Build();

        var callerOptions = new ChatOptions
        {
            Temperature = 0.9f, // Override profile temperature
            TopP = 0.95f // New option not in profile
        };

        var fakeChatClient = new FakeChatClient();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        await _service.GetChatResponseAsync(messages, callerOptions);

        // Assert
        fakeChatClient.ReceivedOptions.Count.ShouldBe(1);
        var receivedOptions = fakeChatClient.ReceivedOptions[0];
        receivedOptions.ShouldNotBeNull();
        receivedOptions!.Temperature.ShouldBe(0.9f); // Caller options take precedence
        receivedOptions.MaxOutputTokens.ShouldBe(1000); // From profile
        receivedOptions.TopP.ShouldBe(0.95f); // From caller
        receivedOptions.ModelId.ShouldBe("gpt-4"); // From profile
    }

    [Fact]
    public async Task GetChatResponseAsync_WithNullOptions_UsesProfileDefaults()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AIProfileBuilder()
            .WithCapability(AICapability.Chat)
            .WithModel("openai", "gpt-4")
            .WithChatSettings(temperature: 0.5f, maxTokens: 500)
            .Build();

        var fakeChatClient = new FakeChatClient();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        await _service.GetChatResponseAsync(messages, null);

        // Assert
        fakeChatClient.ReceivedOptions.Count.ShouldBe(1);
        var receivedOptions = fakeChatClient.ReceivedOptions[0];
        receivedOptions.ShouldNotBeNull();
        receivedOptions!.Temperature.ShouldBe(0.5f);
        receivedOptions.MaxOutputTokens.ShouldBe(500);
        receivedOptions.ModelId.ShouldBe("gpt-4");
    }

    [Fact]
    public async Task GetChatResponseAsync_CallerModelIdOverridesProfileModelId()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AIProfileBuilder()
            .WithCapability(AICapability.Chat)
            .WithModel("openai", "gpt-4")
            .Build();

        var callerOptions = new ChatOptions
        {
            ModelId = "gpt-3.5-turbo"
        };

        var fakeChatClient = new FakeChatClient();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        await _service.GetChatResponseAsync(messages, callerOptions);

        // Assert
        var receivedOptions = fakeChatClient.ReceivedOptions[0];
        receivedOptions!.ModelId.ShouldBe("gpt-3.5-turbo");
    }

    #endregion

    #region GetStreamingChatResponseAsync

    [Fact]
    public async Task GetStreamingChatResponseAsync_WithDefaultProfile_StreamsResponse()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AIProfileBuilder()
            .WithCapability(AICapability.Chat)
            .WithModel("openai", "gpt-4")
            .Build();

        var fakeChatClient = new FakeChatClient("Hello World!");

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in _service.GetStreamingChatResponseAsync(messages))
        {
            updates.Add(update);
        }

        // Assert
        updates.ShouldNotBeEmpty();
        var fullResponse = string.Join("", updates.Select(u => u.Text));
        fullResponse.ShouldContain("Hello");
    }

    [Fact]
    public async Task GetStreamingChatResponseAsync_WithProfileId_StreamsResponse()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithCapability(AICapability.Chat)
            .WithModel("openai", "gpt-4")
            .Build();

        var fakeChatClient = new FakeChatClient("Streamed response");

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in _service.GetStreamingChatResponseAsync(profileId, messages))
        {
            updates.Add(update);
        }

        // Assert
        updates.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetStreamingChatResponseAsync_WithNonExistentProfileId_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in _service.GetStreamingChatResponseAsync(profileId, messages))
            {
                // Should throw before yielding
            }
        });
    }

    [Fact]
    public async Task GetStreamingChatResponseAsync_WithEmbeddingProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var embeddingProfile = new AIProfileBuilder()
            .WithId(profileId)
            .WithCapability(AICapability.Embedding)
            .WithName("Embedding Profile")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingProfile);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in _service.GetStreamingChatResponseAsync(profileId, messages))
            {
                // Should throw before yielding
            }
        });
    }

    #endregion

    #region GetChatClientAsync

    [Fact]
    public async Task GetChatClientAsync_WithNullProfileId_UsesDefaultProfile()
    {
        // Arrange
        var defaultProfile = new AIProfileBuilder()
            .WithCapability(AICapability.Chat)
            .WithModel("openai", "gpt-4")
            .Build();

        var fakeChatClient = new FakeChatClient();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(defaultProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        var client = await _service.GetChatClientAsync();

        // Assert
        client.ShouldBe(fakeChatClient);
        _profileServiceMock.Verify(
            x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetChatClientAsync_WithProfileId_UsesSpecifiedProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithCapability(AICapability.Chat)
            .WithModel("openai", "gpt-4")
            .Build();

        var fakeChatClient = new FakeChatClient();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        var client = await _service.GetChatClientAsync(profileId);

        // Assert
        client.ShouldBe(fakeChatClient);
        _profileServiceMock.Verify(
            x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetChatClientAsync_WithNonExistentProfileId_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        // Act
        var act = () => _service.GetChatClientAsync(profileId);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain($"AI profile with ID '{profileId}' not found");
    }

    [Fact]
    public async Task GetChatClientAsync_WithEmbeddingProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var embeddingProfile = new AIProfileBuilder()
            .WithId(profileId)
            .WithCapability(AICapability.Embedding)
            .WithName("Embedding Profile")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingProfile);

        // Act
        var act = () => _service.GetChatClientAsync(profileId);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("does not support chat capability");
    }

    #endregion
}
