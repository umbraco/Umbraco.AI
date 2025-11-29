using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fakes;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Profile.Controllers;
using Umbraco.Ai.Web.Api.Management.Profile.Models;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Profile;

public class CreateProfileControllerTests
{
    private readonly Mock<IAiProfileRepository> _profileRepositoryMock;
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private List<IAiProvider> _providers = new();

    public CreateProfileControllerTests()
    {
        _profileRepositoryMock = new Mock<IAiProfileRepository>();
        _connectionServiceMock = new Mock<IAiConnectionService>();
    }

    private CreateProfileController CreateController()
    {
        var collection = new AiProviderCollection(() => _providers);
        return new CreateProfileController(
            _profileRepositoryMock.Object,
            _connectionServiceMock.Object,
            collection);
    }

    #region CreateProfile

    [Fact]
    public async Task CreateProfile_WithValidRequest_ReturnsCreatedWithId()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();
        var provider = new FakeAiProvider("openai", "OpenAI");
        _providers.Add(provider);

        var requestModel = new CreateProfileRequestModel
        {
            Alias = "new-profile",
            Name = "New Profile",
            Capability = "Chat",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = connectionId,
            Settings = new ChatProfileSettingsModel { Temperature = 0.7f, MaxTokens = 1000 },
            Tags = new List<string> { "tag1" }
        };

        _profileRepositoryMock
            .Setup(x => x.GetByAliasAsync(requestModel.Alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _profileRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AiProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile p, CancellationToken _) => p);

        var controller = CreateController();

        // Act
        var result = await controller.CreateProfile(requestModel);

        // Assert
        var createdResult = result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.ActionName.ShouldBe(nameof(ByIdProfileController.GetProfileById));
    }

    [Fact]
    public async Task CreateProfile_WithDuplicateAlias_Returns400BadRequest()
    {
        // Arrange
        var existingProfile = new AiProfileBuilder().WithAlias("existing-alias").Build();
        var requestModel = new CreateProfileRequestModel
        {
            Alias = "existing-alias",
            Name = "New Profile",
            Capability = "Chat",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = Guid.NewGuid()
        };

        _profileRepositoryMock
            .Setup(x => x.GetByAliasAsync(requestModel.Alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var controller = CreateController();

        // Act
        var result = await controller.CreateProfile(requestModel);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Duplicate alias");
    }

    [Fact]
    public async Task CreateProfile_WithInvalidCapability_Returns400BadRequest()
    {
        // Arrange
        var requestModel = new CreateProfileRequestModel
        {
            Alias = "new-profile",
            Name = "New Profile",
            Capability = "InvalidCapability",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = Guid.NewGuid()
        };

        var controller = CreateController();

        // Act
        var result = await controller.CreateProfile(requestModel);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Invalid capability");
    }

    [Fact]
    public async Task CreateProfile_WithNonExistingConnection_Returns400BadRequest()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var requestModel = new CreateProfileRequestModel
        {
            Alias = "new-profile",
            Name = "New Profile",
            Capability = "Chat",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = connectionId
        };

        _profileRepositoryMock
            .Setup(x => x.GetByAliasAsync(requestModel.Alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        var controller = CreateController();

        // Act
        var result = await controller.CreateProfile(requestModel);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task CreateProfile_WithNonExistingProvider_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();
        var requestModel = new CreateProfileRequestModel
        {
            Alias = "new-profile",
            Name = "New Profile",
            Capability = "Chat",
            Model = new ModelRefModel { ProviderId = "unknown-provider", ModelId = "model" },
            ConnectionId = connectionId
        };

        _profileRepositoryMock
            .Setup(x => x.GetByAliasAsync(requestModel.Alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        // No providers added

        var controller = CreateController();

        // Act
        var result = await controller.CreateProfile(requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Provider not found");
    }

    [Fact]
    public async Task CreateProfile_MapsRequestToProfile()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();
        var provider = new FakeAiProvider("openai", "OpenAI");
        _providers.Add(provider);

        var requestModel = new CreateProfileRequestModel
        {
            Alias = "test-alias",
            Name = "Test Name",
            Capability = "Chat",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = connectionId,
            Settings = new ChatProfileSettingsModel
            {
                Temperature = 0.5f,
                MaxTokens = 500,
                SystemPromptTemplate = "You are a helpful assistant"
            },
            Tags = new List<string> { "tag1", "tag2" }
        };

        AiProfile? capturedProfile = null;
        _profileRepositoryMock
            .Setup(x => x.GetByAliasAsync(requestModel.Alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _profileRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AiProfile>(), It.IsAny<CancellationToken>()))
            .Callback<AiProfile, CancellationToken>((p, _) => capturedProfile = p)
            .ReturnsAsync((AiProfile p, CancellationToken _) => p);

        var controller = CreateController();

        // Act
        await controller.CreateProfile(requestModel);

        // Assert
        capturedProfile.ShouldNotBeNull();
        capturedProfile!.Id.ShouldNotBe(Guid.Empty);
        capturedProfile.Alias.ShouldBe("test-alias");
        capturedProfile.Name.ShouldBe("Test Name");
        capturedProfile.Capability.ShouldBe(AiCapability.Chat);
        capturedProfile.Model.ProviderId.ShouldBe("openai");
        capturedProfile.Model.ModelId.ShouldBe("gpt-4");
        capturedProfile.ConnectionId.ShouldBe(connectionId);
        capturedProfile.ChatSettings.ShouldNotBeNull();
        capturedProfile.ChatSettings!.Temperature.ShouldBe(0.5f);
        capturedProfile.ChatSettings.MaxTokens.ShouldBe(500);
        capturedProfile.ChatSettings.SystemPromptTemplate.ShouldBe("You are a helpful assistant");
        capturedProfile.Tags.ShouldBe(new[] { "tag1", "tag2" });
    }

    #endregion
}
