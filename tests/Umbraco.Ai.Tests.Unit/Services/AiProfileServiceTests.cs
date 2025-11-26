using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Tests.Common.Builders;

namespace Umbraco.Ai.Tests.Unit.Services;

public class AiProfileServiceTests
{
    private readonly Mock<IAiProfileRepository> _repositoryMock;
    private readonly Mock<IOptions<AiOptions>> _optionsMock;
    private readonly AiProfileService _service;

    public AiProfileServiceTests()
    {
        _repositoryMock = new Mock<IAiProfileRepository>();
        _optionsMock = new Mock<IOptions<AiOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(new AiOptions
        {
            DefaultChatProfileAlias = "default-chat",
            DefaultEmbeddingProfileAlias = "default-embedding"
        });

        _service = new AiProfileService(_repositoryMock.Object, _optionsMock.Object);
    }

    #region GetProfileAsync

    [Fact]
    public async Task GetProfileAsync_WithExistingId_ReturnsProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = new AiProfileBuilder()
            .WithId(profileId)
            .WithAlias("test-profile")
            .WithName("Test Profile")
            .Build();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.GetProfileAsync(profileId);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(profileId);
        result.Alias.ShouldBe("test-profile");
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        // Act
        var result = await _service.GetProfileAsync(profileId);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetProfilesAsync

    [Fact]
    public async Task GetProfilesAsync_WithCapability_ReturnsFilteredProfiles()
    {
        // Arrange
        var chatProfiles = new List<AiProfile>
        {
            new AiProfileBuilder()
                .WithAlias("chat-1")
                .WithCapability(AiCapability.Chat)
                .Build(),
            new AiProfileBuilder()
                .WithAlias("chat-2")
                .WithCapability(AiCapability.Chat)
                .Build()
        };

        _repositoryMock
            .Setup(x => x.GetByCapability(AiCapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatProfiles);

        // Act
        var result = await _service.GetProfilesAsync(AiCapability.Chat);

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldAllBe(p => p.Capability == AiCapability.Chat);
    }

    [Fact]
    public async Task GetProfilesAsync_WithNoMatchingProfiles_ReturnsEmptyCollection()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetByCapability(AiCapability.Embedding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AiProfile>());

        // Act
        var result = await _service.GetProfilesAsync(AiCapability.Embedding);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region GetDefaultProfileAsync

    [Fact]
    public async Task GetDefaultProfileAsync_ForChat_ReturnsDefaultChatProfile()
    {
        // Arrange
        var defaultProfile = new AiProfileBuilder()
            .WithAlias("default-chat")
            .WithCapability(AiCapability.Chat)
            .Build();

        _repositoryMock
            .Setup(x => x.GetByAliasAsync("default-chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        // Act
        var result = await _service.GetDefaultProfileAsync(AiCapability.Chat);

        // Assert
        result.ShouldNotBeNull();
        result.Alias.ShouldBe("default-chat");
    }

    [Fact]
    public async Task GetDefaultProfileAsync_ForEmbedding_ReturnsDefaultEmbeddingProfile()
    {
        // Arrange
        var defaultProfile = new AiProfileBuilder()
            .WithAlias("default-embedding")
            .WithCapability(AiCapability.Embedding)
            .Build();

        _repositoryMock
            .Setup(x => x.GetByAliasAsync("default-embedding", It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        // Act
        var result = await _service.GetDefaultProfileAsync(AiCapability.Embedding);

        // Assert
        result.ShouldNotBeNull();
        result.Alias.ShouldBe("default-embedding");
    }

    [Fact]
    public async Task GetDefaultProfileAsync_WithNoConfiguredAlias_ThrowsInvalidOperationException()
    {
        // Arrange
        var optionsWithNullAlias = new Mock<IOptions<AiOptions>>();
        optionsWithNullAlias.Setup(x => x.Value).Returns(new AiOptions
        {
            DefaultChatProfileAlias = null,
            DefaultEmbeddingProfileAlias = null
        });

        var serviceWithNullOptions = new AiProfileService(
            _repositoryMock.Object,
            optionsWithNullAlias.Object);

        // Act
        var act = () => serviceWithNullOptions.GetDefaultProfileAsync(AiCapability.Chat);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Default Chat profile alias is not configured");
    }

    [Fact]
    public async Task GetDefaultProfileAsync_WithProfileNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetByAliasAsync("default-chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        // Act
        var act = () => _service.GetDefaultProfileAsync(AiCapability.Chat);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Default Chat profile with alias 'default-chat' not found");
    }

    [Fact]
    public async Task GetDefaultProfileAsync_WithUnsupportedCapability_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedCapability = (AiCapability)999;

        // Act
        var act = () => _service.GetDefaultProfileAsync(unsupportedCapability);

        // Assert
        var exception = await Should.ThrowAsync<NotSupportedException>(act);
        exception.Message.ShouldContain("AI capability");
        exception.Message.ShouldContain("999");
        exception.Message.ShouldContain("is not supported");
    }

    #endregion
}
