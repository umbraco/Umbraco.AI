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
    private List<IAiProvider> _providers = new();

    public ModelsConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _mapperMock = new Mock<IUmbracoMapper>();
    }

    private ModelsConnectionController CreateController()
    {
        var collection = new AiProviderCollection(() => _providers);
        return new ModelsConnectionController(
            collection,
            _connectionServiceMock.Object,
            _mapperMock.Object);
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
        var provider = new FakeAiProvider(providerId, "OpenAI")
            .WithChatCapability();
        _providers.Add(provider);

        var responseModels = new List<ModelDescriptorResponseModel>
        {
            new() { Model = new ModelRefModel { ProviderId = providerId, ModelId = "gpt-4" }, Name = "GPT-4" }
        };

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Returns(responseModels);

        var controller = CreateController();

        // Act
        var result = await controller.GetModelsByConnectionId(connectionId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var models = okResult.Value.ShouldBeAssignableTo<IEnumerable<ModelDescriptorResponseModel>>();
        models!.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetModelsByConnectionId_WithNonExistingConnection_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetModelsByConnectionId(connectionId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task GetModelsByConnectionId_WithNonExistingProvider_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("unknown-provider")
            .Build();
        // No providers added

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        var controller = CreateController();

        // Act
        var result = await controller.GetModelsByConnectionId(connectionId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Provider not found");
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

        // Provider with both Chat and Embedding capabilities
        var provider = new FakeAiProvider(providerId, "OpenAI")
            .WithChatCapability(new FakeChatCapability())
            .WithEmbeddingCapability(new FakeEmbeddingCapability());
        _providers.Add(provider);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Returns(new List<ModelDescriptorResponseModel>());

        var controller = CreateController();

        // Act - Filter by Chat capability
        await controller.GetModelsByConnectionId(connectionId, capability: "Chat");

        // Assert - GetModelsAsync should only be called on Chat capability
        // The provider has 2 capabilities, but only Chat should be queried
        _mapperMock.Verify(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(
            It.Is<IEnumerable<AiModelDescriptor>>(models =>
                models.All(m => m.Model.ProviderId == "fake-provider"))), Times.Once);
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
        // No capabilities added
        _providers.Add(provider);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

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
        var provider = new FakeAiProvider(providerId, "OpenAI")
            .WithChatCapability();
        _providers.Add(provider);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

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

        // Create chat capability that returns same model twice
        var duplicateModels = new List<AiModelDescriptor>
        {
            new(new AiModelRef("openai", "gpt-4"), "GPT-4"),
            new(new AiModelRef("openai", "gpt-4"), "GPT-4 Duplicate")
        };
        var chatCapability = new FakeChatCapability(null, duplicateModels);
        var provider = new FakeAiProvider(providerId, "OpenAI")
            .WithChatCapability(chatCapability);
        _providers.Add(provider);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

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
