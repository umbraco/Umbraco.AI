using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Factories;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Services;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Tests.Unit.Services;

public class AiEmbeddingServiceTests
{
    private readonly Mock<IAiEmbeddingGeneratorFactory> _generatorFactoryMock;
    private readonly Mock<IAiProfileService> _profileServiceMock;
    private readonly Mock<IOptionsMonitor<AiOptions>> _optionsMock;
    private readonly AiEmbeddingService _service;

    public AiEmbeddingServiceTests()
    {
        _generatorFactoryMock = new Mock<IAiEmbeddingGeneratorFactory>();
        _profileServiceMock = new Mock<IAiProfileService>();
        _optionsMock = new Mock<IOptionsMonitor<AiOptions>>();
        _optionsMock.Setup(x => x.CurrentValue).Returns(new AiOptions
        {
            DefaultEmbeddingProfileAlias = "default-embedding"
        });

        _service = new AiEmbeddingService(
            _generatorFactoryMock.Object,
            _profileServiceMock.Object,
            _optionsMock.Object);
    }

    #region GenerateEmbeddingAsync - Default profile

    [Fact]
    public async Task GenerateEmbeddingAsync_WithDefaultProfile_UsesDefaultProfile()
    {
        // Arrange
        var value = "Hello world";

        var defaultProfile = new AiProfileBuilder()
            .WithAlias("default-embedding")
            .WithCapability(AiCapability.Embedding)
            .WithModel("openai", "text-embedding-3-small")
            .Build();

        var fakeGenerator = new FakeEmbeddingGenerator();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        _generatorFactoryMock
            .Setup(x => x.CreateGeneratorAsync(defaultProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGenerator);

        // Act
        var embedding = await _service.GenerateEmbeddingAsync(value);

        // Assert
        embedding.ShouldNotBeNull();
        embedding.Vector.ToArray().ShouldBe([0.1f, 0.2f, 0.3f]);
        _profileServiceMock.Verify(
            x => x.GetDefaultProfileAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GenerateEmbeddingAsync - By profile ID

    [Fact]
    public async Task GenerateEmbeddingAsync_WithProfileId_UsesSpecifiedProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var value = "Hello world";

        var profile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Embedding)
            .WithModel("openai", "text-embedding-3-small")
            .Build();

        var fakeGenerator = new FakeEmbeddingGenerator([0.5f, 0.6f, 0.7f]);

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _generatorFactoryMock
            .Setup(x => x.CreateGeneratorAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGenerator);

        // Act
        var embedding = await _service.GenerateEmbeddingAsync(profileId, value);

        // Assert
        embedding.ShouldNotBeNull();
        embedding.Vector.ToArray().ShouldBe([0.5f, 0.6f, 0.7f]);
        _profileServiceMock.Verify(
            x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithNonExistentProfileId_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var value = "Hello world";

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        // Act
        var act = () => _service.GenerateEmbeddingAsync(profileId, value);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain($"AI profile with ID '{profileId}' not found");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithChatProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var value = "Hello world";

        var chatProfile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Chat)
            .WithName("Chat Profile")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatProfile);

        // Act
        var act = () => _service.GenerateEmbeddingAsync(profileId, value);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("does not support embedding capability");
    }

    #endregion

    #region GenerateEmbeddingsAsync - Default profile

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithDefaultProfile_ReturnsEmbeddings()
    {
        // Arrange
        var values = new[] { "Hello", "World" };

        var defaultProfile = new AiProfileBuilder()
            .WithAlias("default-embedding")
            .WithCapability(AiCapability.Embedding)
            .WithModel("openai", "text-embedding-3-small")
            .Build();

        var fakeGenerator = new FakeEmbeddingGenerator();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        _generatorFactoryMock
            .Setup(x => x.CreateGeneratorAsync(defaultProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGenerator);

        // Act
        var embeddings = await _service.GenerateEmbeddingsAsync(values);

        // Assert
        embeddings.ShouldNotBeNull();
        embeddings.Count.ShouldBe(2);
        _profileServiceMock.Verify(
            x => x.GetDefaultProfileAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithMultipleValues_ReturnsEmbeddingForEachValue()
    {
        // Arrange
        var values = new[] { "One", "Two", "Three", "Four" };

        var defaultProfile = new AiProfileBuilder()
            .WithCapability(AiCapability.Embedding)
            .WithModel("openai", "text-embedding-3-small")
            .Build();

        var fakeGenerator = new FakeEmbeddingGenerator();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        _generatorFactoryMock
            .Setup(x => x.CreateGeneratorAsync(defaultProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGenerator);

        // Act
        var embeddings = await _service.GenerateEmbeddingsAsync(values);

        // Assert
        embeddings.ShouldNotBeNull();
        embeddings.Count.ShouldBe(4);
        foreach (var embedding in embeddings)
        {
            embedding.Vector.ToArray().ShouldBe([0.1f, 0.2f, 0.3f]);
        }
    }

    #endregion

    #region GenerateEmbeddingsAsync - By profile ID

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithProfileId_UsesSpecifiedProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var values = new[] { "Hello", "World" };

        var profile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Embedding)
            .WithModel("openai", "text-embedding-3-small")
            .Build();

        var fakeGenerator = new FakeEmbeddingGenerator();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _generatorFactoryMock
            .Setup(x => x.CreateGeneratorAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGenerator);

        // Act
        var embeddings = await _service.GenerateEmbeddingsAsync(profileId, values);

        // Assert
        embeddings.ShouldNotBeNull();
        embeddings.Count.ShouldBe(2);
        _profileServiceMock.Verify(
            x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithNonExistentProfileId_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var values = new[] { "Hello", "World" };

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        // Act
        var act = () => _service.GenerateEmbeddingsAsync(profileId, values);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain($"AI profile with ID '{profileId}' not found");
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithChatProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var values = new[] { "Hello", "World" };

        var chatProfile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Chat)
            .WithName("Chat Profile")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatProfile);

        // Act
        var act = () => _service.GenerateEmbeddingsAsync(profileId, values);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("does not support embedding capability");
    }

    #endregion

    #region GenerateEmbeddingsAsync - Options merging

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithCallerOptions_MergesWithProfileDefaults()
    {
        // Arrange
        var values = new[] { "Hello" };

        var profile = new AiProfileBuilder()
            .WithCapability(AiCapability.Embedding)
            .WithModel("openai", "text-embedding-3-small")
            .Build();

        var callerOptions = new EmbeddingGenerationOptions
        {
            ModelId = "text-embedding-3-large", // Override profile model
            Dimensions = 1024
        };

        var fakeGenerator = new FakeEmbeddingGenerator();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _generatorFactoryMock
            .Setup(x => x.CreateGeneratorAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGenerator);

        // Act
        await _service.GenerateEmbeddingsAsync(values, callerOptions);

        // Assert
        fakeGenerator.ReceivedOptions.Count.ShouldBe(1);
        var receivedOptions = fakeGenerator.ReceivedOptions[0];
        receivedOptions.ShouldNotBeNull();
        receivedOptions!.ModelId.ShouldBe("text-embedding-3-large"); // Caller options take precedence
        receivedOptions.Dimensions.ShouldBe(1024);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithNullOptions_UsesProfileDefaults()
    {
        // Arrange
        var values = new[] { "Hello" };

        var profile = new AiProfileBuilder()
            .WithCapability(AiCapability.Embedding)
            .WithModel("openai", "text-embedding-3-small")
            .Build();

        var fakeGenerator = new FakeEmbeddingGenerator();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _generatorFactoryMock
            .Setup(x => x.CreateGeneratorAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGenerator);

        // Act
        await _service.GenerateEmbeddingsAsync(values, null);

        // Assert
        fakeGenerator.ReceivedOptions.Count.ShouldBe(1);
        var receivedOptions = fakeGenerator.ReceivedOptions[0];
        receivedOptions.ShouldNotBeNull();
        receivedOptions!.ModelId.ShouldBe("text-embedding-3-small");
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_CallerModelIdOverridesProfileModelId()
    {
        // Arrange
        var values = new[] { "Hello" };

        var profile = new AiProfileBuilder()
            .WithCapability(AiCapability.Embedding)
            .WithModel("openai", "text-embedding-3-small")
            .Build();

        var callerOptions = new EmbeddingGenerationOptions
        {
            ModelId = "text-embedding-ada-002"
        };

        var fakeGenerator = new FakeEmbeddingGenerator();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _generatorFactoryMock
            .Setup(x => x.CreateGeneratorAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGenerator);

        // Act
        await _service.GenerateEmbeddingsAsync(values, callerOptions);

        // Assert
        var receivedOptions = fakeGenerator.ReceivedOptions[0];
        receivedOptions!.ModelId.ShouldBe("text-embedding-ada-002");
    }

    #endregion

    #region GetEmbeddingGeneratorAsync

    [Fact]
    public async Task GetEmbeddingGeneratorAsync_WithNullProfileId_UsesDefaultProfile()
    {
        // Arrange
        var defaultProfile = new AiProfileBuilder()
            .WithCapability(AiCapability.Embedding)
            .WithModel("openai", "text-embedding-3-small")
            .Build();

        var fakeGenerator = new FakeEmbeddingGenerator();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        _generatorFactoryMock
            .Setup(x => x.CreateGeneratorAsync(defaultProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGenerator);

        // Act
        var generator = await _service.GetEmbeddingGeneratorAsync();

        // Assert
        generator.ShouldBe(fakeGenerator);
        _profileServiceMock.Verify(
            x => x.GetDefaultProfileAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEmbeddingGeneratorAsync_WithProfileId_UsesSpecifiedProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Embedding)
            .WithModel("openai", "text-embedding-3-small")
            .Build();

        var fakeGenerator = new FakeEmbeddingGenerator();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _generatorFactoryMock
            .Setup(x => x.CreateGeneratorAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGenerator);

        // Act
        var generator = await _service.GetEmbeddingGeneratorAsync(profileId);

        // Assert
        generator.ShouldBe(fakeGenerator);
        _profileServiceMock.Verify(
            x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEmbeddingGeneratorAsync_WithNonExistentProfileId_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        // Act
        var act = () => _service.GetEmbeddingGeneratorAsync(profileId);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain($"AI profile with ID '{profileId}' not found");
    }

    [Fact]
    public async Task GetEmbeddingGeneratorAsync_WithChatProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var chatProfile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Chat)
            .WithName("Chat Profile")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatProfile);

        // Act
        var act = () => _service.GetEmbeddingGeneratorAsync(profileId);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("does not support embedding capability");
    }

    #endregion
}
