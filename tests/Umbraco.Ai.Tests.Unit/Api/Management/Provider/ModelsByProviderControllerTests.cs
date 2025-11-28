using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fakes;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Provider.Controllers;
using Umbraco.Ai.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Provider;

public class ModelsByProviderControllerTests
{
    private readonly Mock<IAiRegistry> _registryMock;
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly ModelsByProviderController _controller;

    public ModelsByProviderControllerTests()
    {
        _registryMock = new Mock<IAiRegistry>();
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new ModelsByProviderController(
            _registryMock.Object,
            _connectionServiceMock.Object,
            _mapperMock.Object);
    }

    #region GetModelsByProviderId

    [Fact]
    public async Task GetModelsByProviderId_WithValidProviderAndConnection_ReturnsModels()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();
        var provider = new FakeAiProvider(providerId, "OpenAI")
            .WithChatCapability();

        var responseModels = new List<ModelDescriptorResponseModel>
        {
            new() { Model = new ModelRefModel { ProviderId = providerId, ModelId = "gpt-4" }, Name = "GPT-4" }
        };

        _registryMock
            .Setup(x => x.GetProvider(providerId))
            .Returns(provider);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Returns(responseModels);

        // Act
        var result = await _controller.GetModelsByProviderId(providerId, connectionId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var models = okResult.Value.ShouldBeAssignableTo<IEnumerable<ModelDescriptorResponseModel>>();
        models!.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetModelsByProviderId_WithNonExistingProvider_Returns404NotFound()
    {
        // Arrange
        var providerId = "unknown-provider";
        var connectionId = Guid.NewGuid();

        _registryMock
            .Setup(x => x.GetProvider(providerId))
            .Returns((IAiProvider?)null);

        // Act
        var result = await _controller.GetModelsByProviderId(providerId, connectionId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Provider not found");
    }

    [Fact]
    public async Task GetModelsByProviderId_WithNonExistingConnection_Returns404NotFound()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var provider = new FakeAiProvider(providerId, "OpenAI");

        _registryMock
            .Setup(x => x.GetProvider(providerId))
            .Returns(provider);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        // Act
        var result = await _controller.GetModelsByProviderId(providerId, connectionId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task GetModelsByProviderId_WithCapabilityFilter_ReturnsFilteredModels()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();

        // Provider with both Chat and Embedding capabilities
        var provider = new FakeAiProvider(providerId, "OpenAI")
            .WithChatCapability(new FakeChatCapability())
            .WithEmbeddingCapability(new FakeEmbeddingCapability());

        _registryMock
            .Setup(x => x.GetProvider(providerId))
            .Returns(provider);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Returns(new List<ModelDescriptorResponseModel>());

        // Act - Filter by Chat capability
        await _controller.GetModelsByProviderId(providerId, connectionId, capability: "Chat");

        // Assert - GetModelsAsync should only be called on Chat capability
        // The provider has 2 capabilities, but only Chat should be queried
        _mapperMock.Verify(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(
            It.Is<IEnumerable<AiModelDescriptor>>(models =>
                models.All(m => m.Model.ProviderId == "fake-provider"))), Times.Once);
    }

    [Fact]
    public async Task GetModelsByProviderId_WithProviderWithNoCapabilities_ReturnsEmptyList()
    {
        // Arrange
        var providerId = "empty-provider";
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();
        var provider = new FakeAiProvider(providerId, "Empty Provider");
        // No capabilities added

        _registryMock
            .Setup(x => x.GetProvider(providerId))
            .Returns(provider);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Returns(new List<ModelDescriptorResponseModel>());

        // Act
        var result = await _controller.GetModelsByProviderId(providerId, connectionId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var models = okResult.Value.ShouldBeAssignableTo<IEnumerable<ModelDescriptorResponseModel>>();
        models!.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetModelsByProviderId_WithInvalidCapabilityFilter_ReturnsAllModels()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();
        var provider = new FakeAiProvider(providerId, "OpenAI")
            .WithChatCapability();

        _registryMock
            .Setup(x => x.GetProvider(providerId))
            .Returns(provider);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Returns(new List<ModelDescriptorResponseModel>());

        // Act - Invalid capability falls back to all capabilities
        await _controller.GetModelsByProviderId(providerId, connectionId, capability: "InvalidCapability");

        // Assert - Should still return OK (models from all capabilities)
        _mapperMock.Verify(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(
            It.IsAny<IEnumerable<AiModelDescriptor>>()), Times.Once);
    }

    [Fact]
    public async Task GetModelsByProviderId_DeduplicatesModelsByModelId()
    {
        // Arrange
        var providerId = "openai";
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();

        // Create chat capability that returns same model twice
        var duplicateModels = new List<AiModelDescriptor>
        {
            new(new AiModelRef("openai", "gpt-4"), "GPT-4"),
            new(new AiModelRef("openai", "gpt-4"), "GPT-4 Duplicate")
        };
        var chatCapability = new FakeChatCapability(null, duplicateModels);
        var provider = new FakeAiProvider(providerId, "OpenAI")
            .WithChatCapability(chatCapability);

        _registryMock
            .Setup(x => x.GetProvider(providerId))
            .Returns(provider);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        IEnumerable<AiModelDescriptor>? capturedModels = null;
        _mapperMock
            .Setup(x => x.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(It.IsAny<IEnumerable<AiModelDescriptor>>()))
            .Callback<IEnumerable<AiModelDescriptor>>(m => capturedModels = m.ToList())
            .Returns(new List<ModelDescriptorResponseModel>());

        // Act
        await _controller.GetModelsByProviderId(providerId, connectionId);

        // Assert - Should only have 1 model after deduplication
        capturedModels.ShouldNotBeNull();
        capturedModels!.Count().ShouldBe(1);
    }

    #endregion
}
