using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fakes;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Connection.Controllers;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Connection;

public class ModelsConnectionControllerTests
{
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;

    public ModelsConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _mapperMock = new Mock<IUmbracoMapper>();
    }

    private ModelsConnectionController CreateController()
    {
        return new ModelsConnectionController(
            _connectionServiceMock.Object,
            _mapperMock.Object);
    }

    private static Mock<IConfiguredProvider> CreateConfiguredProviderMock(
        AiConnection connection,
        IAiProvider provider,
        params IConfiguredCapability[] capabilities)
    {
        var mock = new Mock<IConfiguredProvider>();
        mock.Setup(x => x.Connection).Returns(connection);
        mock.Setup(x => x.Provider).Returns(provider);
        mock.Setup(x => x.GetCapabilities()).Returns(capabilities);
        return mock;
    }

    private static Mock<IConfiguredCapability> CreateConfiguredCapabilityMock(
        AiCapability kind,
        IReadOnlyList<AiModelDescriptor> models)
    {
        var mock = new Mock<IConfiguredCapability>();
        mock.Setup(x => x.Kind).Returns(kind);
        mock.Setup(x => x.GetModelsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(models);
        return mock;
    }

    #region GetModelsByConnectionId

    [Fact]
    public async Task GetModelsByConnectionId_WithValidConnection_ReturnsModels()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(providerId)
            .Build();
        var provider = new FakeAiProvider(providerId, "OpenAI");

        var models = new List<AiModelDescriptor>
        {
            new(new AiModelRef(providerId, "gpt-4"), "GPT-4")
        };

        var capabilityMock = CreateConfiguredCapabilityMock(AiCapability.Chat, models);
        var configuredProviderMock = CreateConfiguredProviderMock(connection, provider, capabilityMock.Object);

        var responseModels = new List<ModelDescriptorResponseModel>
        {
            new() { Model = new ModelRefModel { ProviderId = providerId, ModelId = "gpt-4" }, Name = "GPT-4" }
        };

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Returns(responseModels);

        var controller = CreateController();

        // Act
        var result = await controller.GetModelsByConnectionId(connectionId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedModels = okResult.Value.ShouldBeAssignableTo<IEnumerable<ModelDescriptorResponseModel>>();
        returnedModels!.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetModelsByConnectionId_WithNonExistingConnection_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IConfiguredProvider?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetModelsByConnectionId(connectionId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task GetModelsByConnectionId_WithCapabilityFilter_ReturnsFilteredModels()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(providerId)
            .Build();
        var provider = new FakeAiProvider(providerId, "OpenAI");

        // Provider with both Chat and Embedding capabilities
        var chatModels = new List<AiModelDescriptor>
        {
            new(new AiModelRef(providerId, "gpt-4"), "GPT-4")
        };
        var embeddingModels = new List<AiModelDescriptor>
        {
            new(new AiModelRef(providerId, "text-embedding-3-small"), "Embedding Model")
        };

        var chatCapabilityMock = CreateConfiguredCapabilityMock(AiCapability.Chat, chatModels);
        var embeddingCapabilityMock = CreateConfiguredCapabilityMock(AiCapability.Embedding, embeddingModels);

        var configuredProviderMock = CreateConfiguredProviderMock(
            connection,
            provider,
            chatCapabilityMock.Object,
            embeddingCapabilityMock.Object);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Returns(new List<ModelDescriptorResponseModel>());

        var controller = CreateController();

        // Act - Filter by Chat capability
        await controller.GetModelsByConnectionId(connectionId, capability: "Chat");

        // Assert - Only chat capability's GetModelsAsync should be called
        chatCapabilityMock.Verify(c => c.GetModelsAsync(It.IsAny<CancellationToken>()), Times.Once);
        embeddingCapabilityMock.Verify(c => c.GetModelsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetModelsByConnectionId_WithProviderWithNoCapabilities_ReturnsEmptyList()
    {
        // Arrange
        var providerId = "empty-provider";
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(providerId)
            .Build();
        var provider = new FakeAiProvider(providerId, "Empty Provider");

        // No capabilities
        var configuredProviderMock = CreateConfiguredProviderMock(connection, provider);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Returns(new List<ModelDescriptorResponseModel>());

        var controller = CreateController();

        // Act
        var result = await controller.GetModelsByConnectionId(connectionId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var models = okResult.Value.ShouldBeAssignableTo<IEnumerable<ModelDescriptorResponseModel>>();
        models!.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetModelsByConnectionId_WithInvalidCapabilityFilter_ReturnsAllModels()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(providerId)
            .Build();
        var provider = new FakeAiProvider(providerId, "OpenAI");

        var chatModels = new List<AiModelDescriptor>
        {
            new(new AiModelRef(providerId, "gpt-4"), "GPT-4")
        };

        var chatCapabilityMock = CreateConfiguredCapabilityMock(AiCapability.Chat, chatModels);
        var configuredProviderMock = CreateConfiguredProviderMock(connection, provider, chatCapabilityMock.Object);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Returns(new List<ModelDescriptorResponseModel>());

        var controller = CreateController();

        // Act - Invalid capability falls back to all capabilities
        await controller.GetModelsByConnectionId(connectionId, capability: "InvalidCapability");

        // Assert - Should still return OK (models from all capabilities)
        _mapperMock.Verify(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(
            It.IsAny<IEnumerable<AiModelDescriptor>>()), Times.Once);
    }

    [Fact]
    public async Task GetModelsByConnectionId_DeduplicatesModelsByModelId()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(providerId)
            .Build();
        var provider = new FakeAiProvider(providerId, "OpenAI");

        // Create capability that returns same model twice
        var duplicateModels = new List<AiModelDescriptor>
        {
            new(new AiModelRef("openai", "gpt-4"), "GPT-4"),
            new(new AiModelRef("openai", "gpt-4"), "GPT-4 Duplicate")
        };
        var chatCapabilityMock = CreateConfiguredCapabilityMock(AiCapability.Chat, duplicateModels);
        var configuredProviderMock = CreateConfiguredProviderMock(connection, provider, chatCapabilityMock.Object);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        IEnumerable<AiModelDescriptor>? capturedModels = null;
        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Callback<IEnumerable<AiModelDescriptor>>(m => capturedModels = m.ToList())
            .Returns(new List<ModelDescriptorResponseModel>());

        var controller = CreateController();

        // Act
        await controller.GetModelsByConnectionId(connectionId);

        // Assert - Should only have 1 model after deduplication
        capturedModels.ShouldNotBeNull();
        capturedModels!.Count().ShouldBe(1);
    }

    #endregion
}
