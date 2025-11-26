using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Factories;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Services;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Core.Tests.Services;

public class AiChatServiceTests
{
    private readonly Mock<IAiChatClientFactory> _clientFactoryMock;
    private readonly Mock<IAiProfileService> _profileServiceMock;
    private readonly Mock<IOptionsMonitor<AiOptions>> _optionsMock;
    private readonly AiChatService _service;

    public AiChatServiceTests()
    {
        _clientFactoryMock = new Mock<IAiChatClientFactory>();
        _profileServiceMock = new Mock<IAiProfileService>();
        _optionsMock = new Mock<IOptionsMonitor<AiOptions>>();
        _optionsMock.Setup(x => x.CurrentValue).Returns(new AiOptions
        {
            DefaultChatProfileAlias = "default-chat"
        });

        _service = new AiChatService(
            _clientFactoryMock.Object,
            _profileServiceMock.Object,
            _optionsMock.Object);
    }

    #region GetResponseAsync - Default profile

    [Fact]
    public async Task GetResponseAsync_WithDefaultProfile_UsesDefaultProfile()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var defaultProfile = new AiProfileBuilder()
            .WithAlias("default-chat")
            .WithCapability(AiCapability.Chat)
            .WithModel("openai", "gpt-4")
            .Build();

        var fakeChatClient = new FakeChatClient("Hello! How can I help you?");

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(defaultProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        var response = await _service.GetResponseAsync(messages);

        // Assert
        response.ShouldNotBeNull();
        response.Text.ShouldBe("Hello! How can I help you?");
        _profileServiceMock.Verify(
            x => x.GetDefaultProfileAsync(AiCapability.Chat, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetResponseAsync - By profile ID

    [Fact]
    public async Task GetResponseAsync_WithProfileId_UsesSpecifiedProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Chat)
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
        var response = await _service.GetResponseAsync(profileId, messages);

        // Assert
        response.ShouldNotBeNull();
        response.Text.ShouldBe("Response from specific profile");
        _profileServiceMock.Verify(
            x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_WithNonExistentProfileId_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        // Act
        var act = () => _service.GetResponseAsync(profileId, messages);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain($"AI profile with ID '{profileId}' not found");
    }

    [Fact]
    public async Task GetResponseAsync_WithEmbeddingProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var embeddingProfile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Embedding)
            .WithName("Embedding Profile")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingProfile);

        // Act
        var act = () => _service.GetResponseAsync(profileId, messages);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("does not support chat capability");
    }

    #endregion

    #region GetResponseAsync - Options merging

    [Fact]
    public async Task GetResponseAsync_WithCallerOptions_MergesWithProfileDefaults()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AiProfileBuilder()
            .WithCapability(AiCapability.Chat)
            .WithModel("openai", "gpt-4")
            .WithTemperature(0.7f)
            .WithMaxTokens(1000)
            .Build();

        var callerOptions = new ChatOptions
        {
            Temperature = 0.9f, // Override profile temperature
            TopP = 0.95f // New option not in profile
        };

        var fakeChatClient = new FakeChatClient();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        await _service.GetResponseAsync(messages, callerOptions);

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
    public async Task GetResponseAsync_WithNullOptions_UsesProfileDefaults()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AiProfileBuilder()
            .WithCapability(AiCapability.Chat)
            .WithModel("openai", "gpt-4")
            .WithTemperature(0.5f)
            .WithMaxTokens(500)
            .Build();

        var fakeChatClient = new FakeChatClient();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        await _service.GetResponseAsync(messages, null);

        // Assert
        fakeChatClient.ReceivedOptions.Count.ShouldBe(1);
        var receivedOptions = fakeChatClient.ReceivedOptions[0];
        receivedOptions.ShouldNotBeNull();
        receivedOptions!.Temperature.ShouldBe(0.5f);
        receivedOptions.MaxOutputTokens.ShouldBe(500);
        receivedOptions.ModelId.ShouldBe("gpt-4");
    }

    [Fact]
    public async Task GetResponseAsync_CallerModelIdOverridesProfileModelId()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AiProfileBuilder()
            .WithCapability(AiCapability.Chat)
            .WithModel("openai", "gpt-4")
            .Build();

        var callerOptions = new ChatOptions
        {
            ModelId = "gpt-3.5-turbo"
        };

        var fakeChatClient = new FakeChatClient();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        await _service.GetResponseAsync(messages, callerOptions);

        // Assert
        var receivedOptions = fakeChatClient.ReceivedOptions[0];
        receivedOptions!.ModelId.ShouldBe("gpt-3.5-turbo");
    }

    #endregion

    #region GetStreamingResponseAsync

    [Fact]
    public async Task GetStreamingResponseAsync_WithDefaultProfile_StreamsResponse()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AiProfileBuilder()
            .WithCapability(AiCapability.Chat)
            .WithModel("openai", "gpt-4")
            .Build();

        var fakeChatClient = new FakeChatClient("Hello World!");

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in _service.GetStreamingResponseAsync(messages))
        {
            updates.Add(update);
        }

        // Assert
        updates.ShouldNotBeEmpty();
        var fullResponse = string.Join("", updates.Select(u => u.Text));
        fullResponse.ShouldContain("Hello");
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WithProfileId_StreamsResponse()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var profile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Chat)
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
        await foreach (var update in _service.GetStreamingResponseAsync(profileId, messages))
        {
            updates.Add(update);
        }

        // Assert
        updates.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WithNonExistentProfileId_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in _service.GetStreamingResponseAsync(profileId, messages))
            {
                // Should throw before yielding
            }
        });
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WithEmbeddingProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var embeddingProfile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Embedding)
            .WithName("Embedding Profile")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingProfile);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in _service.GetStreamingResponseAsync(profileId, messages))
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
        var defaultProfile = new AiProfileBuilder()
            .WithCapability(AiCapability.Chat)
            .WithModel("openai", "gpt-4")
            .Build();

        var fakeChatClient = new FakeChatClient();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(defaultProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeChatClient);

        // Act
        var client = await _service.GetChatClientAsync();

        // Assert
        client.ShouldBe(fakeChatClient);
        _profileServiceMock.Verify(
            x => x.GetDefaultProfileAsync(AiCapability.Chat, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetChatClientAsync_WithProfileId_UsesSpecifiedProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Chat)
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
            .ReturnsAsync((AiProfile?)null);

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
        var embeddingProfile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Embedding)
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
