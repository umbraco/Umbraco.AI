using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fakes;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Profile.Controllers;
using Umbraco.Ai.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Profile;

public class UpdateProfileControllerTests
{
    private readonly Mock<IAiProfileService> _profileServiceMock;
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly Mock<IUmbracoMapper> _umbracoMapperMock;
    private List<IAiProvider> _providers = new();

    public UpdateProfileControllerTests()
    {
        _profileServiceMock = new Mock<IAiProfileService>();
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _umbracoMapperMock = new Mock<IUmbracoMapper>();

        // Setup mapper to simulate Map(source, target) behavior
        _umbracoMapperMock
            .Setup(m => m.Map(It.IsAny<UpdateProfileRequestModel>(), It.IsAny<AiProfile>()))
            .Returns((UpdateProfileRequestModel request, AiProfile existing) =>
            {
                // Simulate mapping: update mutable properties, preserve init-only properties (Id, Capability)
                existing.Alias = request.Alias;
                existing.Name = request.Name;
                existing.Model = new AiModelRef(request.Model.ProviderId, request.Model.ModelId);
                existing.ConnectionId = request.ConnectionId;
                existing.Tags = request.Tags ?? Array.Empty<string>();
                return existing;
            });
    }

    private UpdateProfileController CreateController()
    {
        var collection = new AiProviderCollection(() => _providers);
        return new UpdateProfileController(
            _profileServiceMock.Object,
            _connectionServiceMock.Object,
            collection,
            _umbracoMapperMock.Object);
    }

    #region UpdateProfile - By ID

    [Fact]
    public async Task UpdateProfile_WithValidId_ReturnsOk()
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
        _providers.Add(provider);

        var requestModel = new UpdateProfileRequestModel
        {
            Alias = "updated-alias",
            Name = "Updated Name",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = connectionId,
            Settings = new ChatProfileSettingsModel { Temperature = 0.8f, MaxTokens = 2000 }
        };

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _profileServiceMock
            .Setup(x => x.SaveProfileAsync(It.IsAny<AiProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile p, CancellationToken _) => p);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateProfile(new IdOrAlias(profileId), requestModel);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task UpdateProfile_WithNonExistingId_Returns404NotFound()
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

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateProfile(new IdOrAlias(profileId), requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    [Fact]
    public async Task UpdateProfile_WithNonExistingConnection_Returns400BadRequest()
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

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateProfile(new IdOrAlias(profileId), requestModel);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task UpdateProfile_WithNonExistingProvider_Returns404NotFound()
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

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        // No providers added

        var controller = CreateController();

        // Act
        var result = await controller.UpdateProfile(new IdOrAlias(profileId), requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Provider not found");
    }

    [Fact]
    public async Task UpdateProfile_UpdatesAliasFromRequest()
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
        _providers.Add(provider);

        var requestModel = new UpdateProfileRequestModel
        {
            Alias = "updated-alias",
            Name = "Updated Name",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = connectionId
        };

        AiProfile? capturedProfile = null;
        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _profileServiceMock
            .Setup(x => x.SaveProfileAsync(It.IsAny<AiProfile>(), It.IsAny<CancellationToken>()))
            .Callback<AiProfile, CancellationToken>((p, _) => capturedProfile = p)
            .ReturnsAsync((AiProfile p, CancellationToken _) => p);

        var controller = CreateController();

        // Act
        await controller.UpdateProfile(new IdOrAlias(profileId), requestModel);

        // Assert - Alias is updated from request
        capturedProfile.ShouldNotBeNull();
        capturedProfile!.Alias.ShouldBe("updated-alias");
    }

    [Fact]
    public async Task UpdateProfile_PreservesCapabilityFromExisting()
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
        _providers.Add(provider);

        var requestModel = new UpdateProfileRequestModel
        {
            Alias = "updated-alias",
            Name = "Updated Name",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "model" },
            ConnectionId = connectionId
        };

        AiProfile? capturedProfile = null;
        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _profileServiceMock
            .Setup(x => x.SaveProfileAsync(It.IsAny<AiProfile>(), It.IsAny<CancellationToken>()))
            .Callback<AiProfile, CancellationToken>((p, _) => capturedProfile = p)
            .ReturnsAsync((AiProfile p, CancellationToken _) => p);

        var controller = CreateController();

        // Act
        await controller.UpdateProfile(new IdOrAlias(profileId), requestModel);

        // Assert - Capability should be preserved from existing profile
        capturedProfile.ShouldNotBeNull();
        capturedProfile!.Capability.ShouldBe(AiCapability.Embedding);
    }

    #endregion

    #region UpdateProfile - By Alias

    [Fact]
    public async Task UpdateProfile_WithValidAlias_ReturnsOk()
    {
        // Arrange
        var alias = "my-profile";
        var profileId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var existingProfile = new AiProfileBuilder()
            .WithId(profileId)
            .WithAlias(alias)
            .WithCapability(AiCapability.Chat)
            .Build();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();
        var provider = new FakeAiProvider("openai", "OpenAI");
        _providers.Add(provider);

        var requestModel = new UpdateProfileRequestModel
        {
            Alias = "updated-alias",
            Name = "Updated Name",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = connectionId,
            Settings = new ChatProfileSettingsModel { Temperature = 0.8f, MaxTokens = 2000 }
        };

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _profileServiceMock
            .Setup(x => x.SaveProfileAsync(It.IsAny<AiProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile p, CancellationToken _) => p);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateProfile(new IdOrAlias(alias), requestModel);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task UpdateProfile_WithNonExistingAlias_Returns404NotFound()
    {
        // Arrange
        var alias = "non-existing";
        var requestModel = new UpdateProfileRequestModel
        {
            Alias = "updated-alias",
            Name = "Updated Name",
            Model = new ModelRefModel { ProviderId = "openai", ModelId = "gpt-4" },
            ConnectionId = Guid.NewGuid()
        };

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateProfile(new IdOrAlias(alias), requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    #endregion
}
