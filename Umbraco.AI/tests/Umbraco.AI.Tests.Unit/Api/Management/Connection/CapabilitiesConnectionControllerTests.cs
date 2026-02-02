using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Connection.Controllers;

namespace Umbraco.AI.Tests.Unit.Api.Management.Connection;

public class CapabilitiesConnectionControllerTests
{
    private readonly Mock<IAIConnectionService> _connectionServiceMock;

    public CapabilitiesConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAIConnectionService>();
    }

    private CapabilitiesConnectionController CreateController()
    {
        return new CapabilitiesConnectionController(_connectionServiceMock.Object);
    }

    private static Mock<IAIConfiguredProvider> CreateConfiguredProviderMock(
        IAIProvider provider,
        params IAIConfiguredCapability[] capabilities)
    {
        var mock = new Mock<IAIConfiguredProvider>();
        mock.Setup(x => x.Provider).Returns(provider);
        mock.Setup(x => x.GetCapabilities()).Returns(capabilities);
        return mock;
    }

    private static Mock<IAIConfiguredCapability> CreateConfiguredCapabilityMock(AICapability kind)
    {
        var mock = new Mock<IAIConfiguredCapability>();
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

        var chatCapabilityMock = CreateConfiguredCapabilityMock(AICapability.Chat);
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
            .ReturnsAsync((IAIConfiguredProvider?)null);

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

        var chatCapabilityMock = CreateConfiguredCapabilityMock(AICapability.Chat);
        var embeddingCapabilityMock = CreateConfiguredCapabilityMock(AICapability.Embedding);
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
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithAlias(alias)
            .Build();
        var provider = new FakeAiProvider("openai", "OpenAI");

        var chatCapabilityMock = CreateConfiguredCapabilityMock(AICapability.Chat);
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
            .ReturnsAsync((AIConnection?)null);

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
