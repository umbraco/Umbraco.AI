using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.AI.Web.Api.Management.Profile.Controllers;
using Umbraco.AI.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Profile;

public class CreateProfileControllerTests
{
    private readonly Mock<IAIProfileService> _profileServiceMock;
    private readonly Mock<IAIConnectionService> _connectionServiceMock;
    private readonly Mock<IUmbracoMapper> _umbracoMapperMock;
    private List<IAIProvider> _providers = new();

    public CreateProfileControllerTests()
    {
        _profileServiceMock = new Mock<IAIProfileService>();
        _connectionServiceMock = new Mock<IAIConnectionService>();
        _umbracoMapperMock = new Mock<IUmbracoMapper>();

        // Setup mapper to use real mapping logic
        _umbracoMapperMock
            .Setup(m => m.Map<AIProfile>(It.IsAny<CreateProfileRequestModel>()))
            .Returns((CreateProfileRequestModel request) =>
            {
                var capability = Enum.TryParse<AICapability>(request.Capability, true, out var cap)
                    ? cap
                    : AICapability.Chat;
                return new AIProfile
                {
                    Id = Guid.Empty,
                    Alias = request.Alias,
                    Name = request.Name,
                    Capability = capability,
                    Model = new AIModelRef(request.Model.ProviderId, request.Model.ModelId),
                    ConnectionId = request.ConnectionId,
                    Settings = MapSettingsFromRequest(capability, request.Settings),
                    Tags = request.Tags
                };
            });
    }

    private static IAIProfileSettings? MapSettingsFromRequest(AICapability capability, ProfileSettingsModel? settings)
    {
        return capability switch
        {
            AICapability.Chat when settings is ChatProfileSettingsModel chat => new AIChatProfileSettings
            {
                Temperature = chat.Temperature,
                MaxTokens = chat.MaxTokens,
                SystemPromptTemplate = chat.SystemPromptTemplate
            },
            AICapability.Chat => new AIChatProfileSettings(),
            AICapability.Embedding => new AIEmbeddingProfileSettings(),
            _ => null
        };
    }

    private CreateProfileController CreateController()
    {
        var collection = new AIProviderCollection(() => _providers);
        return new CreateProfileController(
            _profileServiceMock.Object,
            _connectionServiceMock.Object,
            collection,
            _umbracoMapperMock.Object);
    }

    #region CreateProfile

    [Fact]
    public async Task CreateProfile_WithValidRequest_ReturnsCreatedWithId()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder().WithId(connectionId).Build();
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

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(requestModel.Alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _profileServiceMock
            .Setup(x => x.SaveProfileAsync(It.IsAny<AIProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile p, CancellationToken _) => p);

        var controller = CreateController();

        // Act
        var result = await controller.CreateProfile(requestModel);

        // Assert
        var createdResult = result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.ActionName.ShouldBe(nameof(ByIdOrAliasProfileController.GetProfileByIdOrAlias));
    }

    [Fact]
    public async Task CreateProfile_WithDuplicateAlias_Returns400BadRequest()
    {
        // Arrange
        var existingProfile = new AIProfileBuilder().WithAlias("existing-alias").Build();
        var requestModel = new CreateProfileRequestModel
        {
            Alias = "existing-alias",
            Name = "New Profile",
            Capability = "Chat",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = Guid.NewGuid()
        };

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(requestModel.Alias, It.IsAny<CancellationToken>()))
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

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(requestModel.Alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection?)null);

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
        var connection = new AIConnectionBuilder().WithId(connectionId).Build();
        var requestModel = new CreateProfileRequestModel
        {
            Alias = "new-profile",
            Name = "New Profile",
            Capability = "Chat",
            Model = new ModelRefModel { ProviderId = "unknown-provider", ModelId = "model" },
            ConnectionId = connectionId
        };

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(requestModel.Alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

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
        var connection = new AIConnectionBuilder().WithId(connectionId).Build();
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

        AIProfile? capturedProfile = null;
        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(requestModel.Alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _profileServiceMock
            .Setup(x => x.SaveProfileAsync(It.IsAny<AIProfile>(), It.IsAny<CancellationToken>()))
            .Callback<AIProfile, CancellationToken>((p, _) => capturedProfile = p)
            .ReturnsAsync((AIProfile p, CancellationToken _) => p);

        var controller = CreateController();

        // Act
        await controller.CreateProfile(requestModel);

        // Assert - Controller passes Guid.Empty, service/repository generates the ID
        capturedProfile.ShouldNotBeNull();
        capturedProfile!.Id.ShouldBe(Guid.Empty);
        capturedProfile.Alias.ShouldBe("test-alias");
        capturedProfile.Name.ShouldBe("Test Name");
        capturedProfile.Capability.ShouldBe(AICapability.Chat);
        capturedProfile.Model.ProviderId.ShouldBe("openai");
        capturedProfile.Model.ModelId.ShouldBe("gpt-4");
        capturedProfile.ConnectionId.ShouldBe(connectionId);
        capturedProfile.Settings.ShouldNotBeNull();
        
        var chatSettings = capturedProfile.Settings as AIChatProfileSettings;
        chatSettings.ShouldNotBeNull();
        chatSettings!.Temperature.ShouldBe(0.5f);
        chatSettings.MaxTokens.ShouldBe(500);
        chatSettings.SystemPromptTemplate.ShouldBe("You are a helpful assistant");
        
        capturedProfile.Tags.ShouldBe(new[] { "tag1", "tag2" });
    }

    #endregion
}
