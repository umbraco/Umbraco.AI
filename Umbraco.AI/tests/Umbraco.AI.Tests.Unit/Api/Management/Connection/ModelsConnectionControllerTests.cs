using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.AI.Web.Api.Management.Connection.Controllers;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Connection;

public class ModelsConnectionControllerTests
{
    private readonly Mock<IAIConnectionService> _connectionServiceMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;

    public ModelsConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAIConnectionService>();
        _mapperMock = new Mock<IUmbracoMapper>();
    }

    private ModelsConnectionController CreateController()
    {
        return new ModelsConnectionController(
            _connectionServiceMock.Object,
            _mapperMock.Object);
    }

    private static Mock<IAIConfiguredProvider> CreateConfiguredProviderMock(
        AIConnection connection,
        IAIProvider provider,
        params IAIConfiguredCapability[] capabilities)
    {
        var mock = new Mock<IAIConfiguredProvider>();
        mock.Setup(x => x.Provider).Returns(provider);
        mock.Setup(x => x.GetCapabilities()).Returns(capabilities);
        return mock;
    }

    private static Mock<IAIConfiguredCapability> CreateConfiguredCapabilityMock(
        AICapability kind,
        IReadOnlyList<AIModelDescriptor> models)
    {
        var mock = new Mock<IAIConfiguredCapability>();
        mock.Setup(x => x.Kind).Returns(kind);
        mock.Setup(x => x.GetModelsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(models);
        return mock;
    }

    #region GetModels - By ID

    [Fact]
    public async Task GetModels_WithValidId_ReturnsModels()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(providerId)
            .Build();
        var provider = new FakeAIProvider(providerId, "OpenAI");

        var models = new List<AIModelDescriptor>
        {
            new(new AIModelRef(providerId, "gpt-4"), "GPT-4")
        };

        var capabilityMock = CreateConfiguredCapabilityMock(AICapability.Chat, models);
        var configuredProviderMock = CreateConfiguredProviderMock(connection, provider, capabilityMock.Object);

        var responseModels = new List<ModelDescriptorResponseModel>
        {
            new() { Model = new ModelRefModel { ProviderId = providerId, ModelId = "gpt-4" }, Name = "GPT-4" }
        };

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        _mapperMock
            .Setup(x => x.MapEnumerable<AIModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AIModelDescriptor>>()))
            .Returns(responseModels);

        var controller = CreateController();

        // Act
        var result = await controller.GetModels(new IdOrAlias(connectionId));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedModels = okResult.Value.ShouldBeAssignableTo<IEnumerable<ModelDescriptorResponseModel>>();
        returnedModels!.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetModels_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAIConfiguredProvider?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetModels(new IdOrAlias(connectionId));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task GetModels_WithCapabilityFilter_ReturnsFilteredModels()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(providerId)
            .Build();
        var provider = new FakeAIProvider(providerId, "OpenAI");

        // Provider with both Chat and Embedding capabilities
        var chatModels = new List<AIModelDescriptor>
        {
            new(new AIModelRef(providerId, "gpt-4"), "GPT-4")
        };
        var embeddingModels = new List<AIModelDescriptor>
        {
            new(new AIModelRef(providerId, "text-embedding-3-small"), "Embedding Model")
        };

        var chatCapabilityMock = CreateConfiguredCapabilityMock(AICapability.Chat, chatModels);
        var embeddingCapabilityMock = CreateConfiguredCapabilityMock(AICapability.Embedding, embeddingModels);

        var configuredProviderMock = CreateConfiguredProviderMock(
            connection,
            provider,
            chatCapabilityMock.Object,
            embeddingCapabilityMock.Object);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        _mapperMock
            .Setup(x => x.MapEnumerable<AIModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AIModelDescriptor>>()))
            .Returns(new List<ModelDescriptorResponseModel>());

        var controller = CreateController();

        // Act - Filter by Chat capability
        await controller.GetModels(new IdOrAlias(connectionId), capability: "Chat");

        // Assert - Only chat capability's GetModelsAsync should be called
        chatCapabilityMock.Verify(c => c.GetModelsAsync(It.IsAny<CancellationToken>()), Times.Once);
        embeddingCapabilityMock.Verify(c => c.GetModelsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetModels_WithProviderWithNoCapabilities_ReturnsEmptyList()
    {
        // Arrange
        var providerId = "empty-provider";
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(providerId)
            .Build();
        var provider = new FakeAIProvider(providerId, "Empty Provider");

        // No capabilities
        var configuredProviderMock = CreateConfiguredProviderMock(connection, provider);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        _mapperMock
            .Setup(x => x.MapEnumerable<AIModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AIModelDescriptor>>()))
            .Returns(new List<ModelDescriptorResponseModel>());

        var controller = CreateController();

        // Act
        var result = await controller.GetModels(new IdOrAlias(connectionId));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var models = okResult.Value.ShouldBeAssignableTo<IEnumerable<ModelDescriptorResponseModel>>();
        models!.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetModels_WithInvalidCapabilityFilter_ReturnsAllModels()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(providerId)
            .Build();
        var provider = new FakeAIProvider(providerId, "OpenAI");

        var chatModels = new List<AIModelDescriptor>
        {
            new(new AIModelRef(providerId, "gpt-4"), "GPT-4")
        };

        var chatCapabilityMock = CreateConfiguredCapabilityMock(AICapability.Chat, chatModels);
        var configuredProviderMock = CreateConfiguredProviderMock(connection, provider, chatCapabilityMock.Object);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        _mapperMock
            .Setup(x => x.MapEnumerable<AIModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AIModelDescriptor>>()))
            .Returns(new List<ModelDescriptorResponseModel>());

        var controller = CreateController();

        // Act - Invalid capability falls back to all capabilities
        await controller.GetModels(new IdOrAlias(connectionId), capability: "InvalidCapability");

        // Assert - Should still return OK (models from all capabilities)
        _mapperMock.Verify(x => x.MapEnumerable<AIModelDescriptor, ModelDescriptorResponseModel>(
            It.IsAny<IEnumerable<AIModelDescriptor>>()), Times.Once);
    }

    [Fact]
    public async Task GetModels_DeduplicatesModelsByModelId()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(providerId)
            .Build();
        var provider = new FakeAIProvider(providerId, "OpenAI");

        // Create capability that returns same model twice
        var duplicateModels = new List<AIModelDescriptor>
        {
            new(new AIModelRef("openai", "gpt-4"), "GPT-4"),
            new(new AIModelRef("openai", "gpt-4"), "GPT-4 Duplicate")
        };
        var chatCapabilityMock = CreateConfiguredCapabilityMock(AICapability.Chat, duplicateModels);
        var configuredProviderMock = CreateConfiguredProviderMock(connection, provider, chatCapabilityMock.Object);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        IEnumerable<AIModelDescriptor>? capturedModels = null;
        _mapperMock
            .Setup(x => x.MapEnumerable<AIModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AIModelDescriptor>>()))
            .Callback<IEnumerable<AIModelDescriptor>>(m => capturedModels = m.ToList())
            .Returns(new List<ModelDescriptorResponseModel>());

        var controller = CreateController();

        // Act
        await controller.GetModels(new IdOrAlias(connectionId));

        // Assert - Should only have 1 model after deduplication
        capturedModels.ShouldNotBeNull();
        capturedModels!.Count().ShouldBe(1);
    }

    #endregion

    #region GetModels - By Alias

    [Fact]
    public async Task GetModels_WithValidAlias_ReturnsModels()
    {
        // Arrange
        var alias = "my-connection";
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithAlias(alias)
            .WithProviderId(providerId)
            .Build();
        var provider = new FakeAIProvider(providerId, "OpenAI");

        var models = new List<AIModelDescriptor>
        {
            new(new AIModelRef(providerId, "gpt-4"), "GPT-4")
        };

        var capabilityMock = CreateConfiguredCapabilityMock(AICapability.Chat, models);
        var configuredProviderMock = CreateConfiguredProviderMock(connection, provider, capabilityMock.Object);

        var responseModels = new List<ModelDescriptorResponseModel>
        {
            new() { Model = new ModelRefModel { ProviderId = providerId, ModelId = "gpt-4" }, Name = "GPT-4" }
        };

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        _mapperMock
            .Setup(x => x.MapEnumerable<AIModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AIModelDescriptor>>()))
            .Returns(responseModels);

        var controller = CreateController();

        // Act
        var result = await controller.GetModels(new IdOrAlias(alias));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedModels = okResult.Value.ShouldBeAssignableTo<IEnumerable<ModelDescriptorResponseModel>>();
        returnedModels!.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetModels_WithNonExistingAlias_Returns404NotFound()
    {
        // Arrange
        var alias = "non-existing";

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetModels(new IdOrAlias(alias));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    #endregion
}
