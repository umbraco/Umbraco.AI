using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fakes;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Connection.Controllers;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Connection;

public class CapabilitiesConnectionControllerTests
{
    private readonly Mock<IAiConnectionService> _connectionServiceMock;

    public CapabilitiesConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
    }

    private CapabilitiesConnectionController CreateController()
    {
        return new CapabilitiesConnectionController(_connectionServiceMock.Object);
    }

    private static Mock<IAiConfiguredProvider> CreateConfiguredProviderMock(
        IAiProvider provider,
        params IAiConfiguredCapability[] capabilities)
    {
        var mock = new Mock<IAiConfiguredProvider>();
        mock.Setup(x => x.Provider).Returns(provider);
        mock.Setup(x => x.GetCapabilities()).Returns(capabilities);
        return mock;
    }

    private static Mock<IAiConfiguredCapability> CreateConfiguredCapabilityMock(AiCapability kind)
    {
        var mock = new Mock<IAiConfiguredCapability>();
        mock.Setup(x => x.Kind).Returns(kind);
        return mock;
    }

    #region GetCapabilities - By ID

    [Fact]
    public async Task GetCapabilities_WithValidId_ReturnsCapabilities()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var provider = new FakeAiProvider("openai", "OpenAI");

        var chatCapabilityMock = CreateConfiguredCapabilityMock(AiCapability.Chat);
        var configuredProviderMock = CreateConfiguredProviderMock(provider, chatCapabilityMock.Object);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        var controller = CreateController();

        // Act
        var result = await controller.GetCapabilities(new IdOrAlias(connectionId));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var capabilities = okResult.Value.ShouldBeAssignableTo<IEnumerable<string>>();
        capabilities!.ShouldContain("Chat");
    }

    [Fact]
    public async Task GetCapabilities_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAiConfiguredProvider?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetCapabilities(new IdOrAlias(connectionId));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task GetCapabilities_WithMultipleCapabilities_ReturnsAllCapabilities()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var provider = new FakeAiProvider("openai", "OpenAI");

        var chatCapabilityMock = CreateConfiguredCapabilityMock(AiCapability.Chat);
        var embeddingCapabilityMock = CreateConfiguredCapabilityMock(AiCapability.Embedding);
        var configuredProviderMock = CreateConfiguredProviderMock(
            provider,
            chatCapabilityMock.Object,
            embeddingCapabilityMock.Object);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        var controller = CreateController();

        // Act
        var result = await controller.GetCapabilities(new IdOrAlias(connectionId));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var capabilities = okResult.Value.ShouldBeAssignableTo<IEnumerable<string>>()!.ToList();
        capabilities.ShouldContain("Chat");
        capabilities.ShouldContain("Embedding");
        capabilities.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetCapabilities_WithNoCapabilities_ReturnsEmptyList()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var provider = new FakeAiProvider("empty-provider", "Empty Provider");

        var configuredProviderMock = CreateConfiguredProviderMock(provider);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        var controller = CreateController();

        // Act
        var result = await controller.GetCapabilities(new IdOrAlias(connectionId));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var capabilities = okResult.Value.ShouldBeAssignableTo<IEnumerable<string>>();
        capabilities!.ShouldBeEmpty();
    }

    #endregion

    #region GetCapabilities - By Alias

    [Fact]
    public async Task GetCapabilities_WithValidAlias_ReturnsCapabilities()
    {
        // Arrange
        var alias = "my-connection";
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithAlias(alias)
            .Build();
        var provider = new FakeAiProvider("openai", "OpenAI");

        var chatCapabilityMock = CreateConfiguredCapabilityMock(AiCapability.Chat);
        var configuredProviderMock = CreateConfiguredProviderMock(provider, chatCapabilityMock.Object);

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        var controller = CreateController();

        // Act
        var result = await controller.GetCapabilities(new IdOrAlias(alias));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var capabilities = okResult.Value.ShouldBeAssignableTo<IEnumerable<string>>();
        capabilities!.ShouldContain("Chat");
    }

    [Fact]
    public async Task GetCapabilities_WithNonExistingAlias_Returns404NotFound()
    {
        // Arrange
        var alias = "non-existing";

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetCapabilities(new IdOrAlias(alias));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    #endregion
}
