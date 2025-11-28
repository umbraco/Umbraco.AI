using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fakes;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Profile.Controllers;
using Umbraco.Ai.Web.Api.Management.Profile.Models;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Profile;

public class UpdateProfileControllerTests
{
    private readonly Mock<IAiProfileRepository> _profileRepositoryMock;
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly Mock<IAiRegistry> _registryMock;
    private readonly UpdateProfileController _controller;

    public UpdateProfileControllerTests()
    {
        _profileRepositoryMock = new Mock<IAiProfileRepository>();
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _registryMock = new Mock<IAiRegistry>();

        _controller = new UpdateProfileController(
            _profileRepositoryMock.Object,
            _connectionServiceMock.Object,
            _registryMock.Object);
    }

    #region UpdateProfileById

    [Fact]
    public async Task UpdateProfileById_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var existingProfile = new AiProfileBuilder()
            .WithId(profileId)
            .WithAlias("original-alias")
            .WithCapability(AiCapability.Chat)
            .Build();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();
        var provider = new FakeAiProvider("openai", "OpenAI");

        var requestModel = new UpdateProfileRequestModel
        {
            Alias = "updated-alias",
            Name = "Updated Name",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = connectionId,
            Temperature = 0.8f,
            MaxTokens = 2000
        };

        _profileRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _registryMock
            .Setup(x => x.GetProvider("openai"))
            .Returns(provider);

        _profileRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AiProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile p, CancellationToken _) => p);

        // Act
        var result = await _controller.UpdateProfileById(profileId, requestModel);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task UpdateProfileById_WithNonExistingProfile_Returns404NotFound()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var requestModel = new UpdateProfileRequestModel
        {
            Alias = "updated-alias",
            Name = "Updated Name",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = Guid.NewGuid()
        };

        _profileRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        // Act
        var result = await _controller.UpdateProfileById(profileId, requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    [Fact]
    public async Task UpdateProfileById_WithNonExistingConnection_Returns400BadRequest()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var existingProfile = new AiProfileBuilder().WithId(profileId).Build();

        var requestModel = new UpdateProfileRequestModel
        {
            Alias = "updated-alias",
            Name = "Updated Name",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = connectionId
        };

        _profileRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        // Act
        var result = await _controller.UpdateProfileById(profileId, requestModel);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task UpdateProfileById_WithNonExistingProvider_Returns404NotFound()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var existingProfile = new AiProfileBuilder().WithId(profileId).Build();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();

        var requestModel = new UpdateProfileRequestModel
        {
            Alias = "updated-alias",
            Name = "Updated Name",
            Model = new ModelRefModel { ProviderId = "unknown-provider", ModelId = "model" },
            ConnectionId = connectionId
        };

        _profileRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _registryMock
            .Setup(x => x.GetProvider("unknown-provider"))
            .Returns((FakeAiProvider?)null);

        // Act
        var result = await _controller.UpdateProfileById(profileId, requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Provider not found");
    }

    [Fact]
    public async Task UpdateProfileById_PreservesAliasFromExisting()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var existingProfile = new AiProfileBuilder()
            .WithId(profileId)
            .WithAlias("original-alias")
            .Build();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();
        var provider = new FakeAiProvider("openai", "OpenAI");

        var requestModel = new UpdateProfileRequestModel
        {
            Alias = "updated-alias",
            Name = "Updated Name",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = connectionId
        };

        AiProfile? capturedProfile = null;
        _profileRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _registryMock
            .Setup(x => x.GetProvider("openai"))
            .Returns(provider);

        _profileRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AiProfile>(), It.IsAny<CancellationToken>()))
            .Callback<AiProfile, CancellationToken>((p, _) => capturedProfile = p)
            .ReturnsAsync((AiProfile p, CancellationToken _) => p);

        // Act
        await _controller.UpdateProfileById(profileId, requestModel);

        // Assert - Alias should be preserved from existing profile
        capturedProfile.ShouldNotBeNull();
        capturedProfile!.Alias.ShouldBe("original-alias");
    }

    [Fact]
    public async Task UpdateProfileById_PreservesCapabilityFromExisting()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var existingProfile = new AiProfileBuilder()
            .WithId(profileId)
            .WithCapability(AiCapability.Embedding)
            .Build();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();
        var provider = new FakeAiProvider("openai", "OpenAI");

        var requestModel = new UpdateProfileRequestModel
        {
            Alias = "updated-alias",
            Name = "Updated Name",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "model" },
            ConnectionId = connectionId
        };

        AiProfile? capturedProfile = null;
        _profileRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _registryMock
            .Setup(x => x.GetProvider("openai"))
            .Returns(provider);

        _profileRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AiProfile>(), It.IsAny<CancellationToken>()))
            .Callback<AiProfile, CancellationToken>((p, _) => capturedProfile = p)
            .ReturnsAsync((AiProfile p, CancellationToken _) => p);

        // Act
        await _controller.UpdateProfileById(profileId, requestModel);

        // Assert - Capability should be preserved from existing profile
        capturedProfile.ShouldNotBeNull();
        capturedProfile!.Capability.ShouldBe(AiCapability.Embedding);
    }

    #endregion
}
