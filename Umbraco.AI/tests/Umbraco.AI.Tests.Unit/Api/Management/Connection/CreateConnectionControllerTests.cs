using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Web.Api.Management.Connection.Controllers;
using Umbraco.AI.Web.Api.Management.Connection.Mapping;
using Umbraco.AI.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Connection;

public class CreateConnectionControllerTests
{
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly Mock<IUmbracoMapper> _umbracoMapperMock;
    private readonly CreateConnectionController _controller;

    public CreateConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _umbracoMapperMock = new Mock<IUmbracoMapper>();

        // Setup mapper to use real mapping logic
        _umbracoMapperMock
            .Setup(m => m.Map<AIConnection>(It.IsAny<CreateConnectionRequestModel>()))
            .Returns((CreateConnectionRequestModel request) => new AIConnection
            {
                Id = Guid.Empty,
                Alias = request.Alias,
                Name = request.Name,
                ProviderId = request.ProviderId,
                Settings = request.Settings,
                IsActive = request.IsActive
            });

        _controller = new CreateConnectionController(_connectionServiceMock.Object, _umbracoMapperMock.Object);
    }

    #region CreateConnection

    [Fact]
    public async Task CreateConnection_WithValidRequest_ReturnsCreatedWithId()
    {
        // Arrange
        var requestModel = new CreateConnectionRequestModel
        {
            Alias = "new-connection",
            Name = "New Connection",
            ProviderId = "openai",
            Settings = new { ApiKey = "test-key" },
            IsActive = true
        };

        var createdId = Guid.NewGuid();
        var createdConnection = new AIConnectionBuilder()
            .WithId(createdId)
            .WithName("New Connection")
            .WithProviderId("openai")
            .Build();

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AIConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdConnection);

        // Act
        var result = await _controller.CreateConnection(requestModel);

        // Assert
        var createdResult = result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.ActionName.ShouldBe(nameof(ByIdOrAliasConnectionController.GetConnectionByIdOrAlias));
        createdResult.Value.ShouldBe(createdId.ToString());
    }

    [Fact]
    public async Task CreateConnection_WithProviderNotFound_Returns404NotFound()
    {
        // Arrange
        var requestModel = new CreateConnectionRequestModel
        {
            Alias = "new-connection",
            Name = "New Connection",
            ProviderId = "unknown-provider",
            IsActive = true
        };

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AIConnection>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider 'unknown-provider' not found in registry"));

        // Act
        var result = await _controller.CreateConnection(requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Provider not found");
    }

    [Fact]
    public async Task CreateConnection_WithInvalidSettings_Returns400BadRequest()
    {
        // Arrange
        var requestModel = new CreateConnectionRequestModel
        {
            Alias = "new-connection",
            Name = "New Connection",
            ProviderId = "openai",
            Settings = new { InvalidKey = "value" },
            IsActive = true
        };

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AIConnection>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Settings validation failed: API Key is required"));

        // Act
        var result = await _controller.CreateConnection(requestModel);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Invalid settings");
    }

    [Fact]
    public async Task CreateConnection_SetsConnectionIdToEmpty()
    {
        // Arrange
        var requestModel = new CreateConnectionRequestModel
        {
            Alias = "new-connection",
            Name = "New Connection",
            ProviderId = "openai",
            IsActive = true
        };

        AIConnection? capturedConnection = null;
        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AIConnection>(), It.IsAny<CancellationToken>()))
            .Callback<AIConnection, CancellationToken>((conn, _) => capturedConnection = conn)
            .ReturnsAsync((AIConnection conn, CancellationToken _) => conn);

        // Act
        await _controller.CreateConnection(requestModel);

        // Assert - Controller should pass Guid.Empty, service generates the ID
        capturedConnection.ShouldNotBeNull();
        capturedConnection!.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateConnection_MapsRequestModelToConnection()
    {
        // Arrange
        var settings = new { ApiKey = "test-key" };
        var requestModel = new CreateConnectionRequestModel
        {
            Alias = "new-connection",
            Name = "New Connection",
            ProviderId = "openai",
            Settings = settings,
            IsActive = false
        };

        AIConnection? capturedConnection = null;
        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AIConnection>(), It.IsAny<CancellationToken>()))
            .Callback<AIConnection, CancellationToken>((conn, _) => capturedConnection = conn)
            .ReturnsAsync((AIConnection conn, CancellationToken _) => conn);

        // Act
        await _controller.CreateConnection(requestModel);

        // Assert
        capturedConnection.ShouldNotBeNull();
        capturedConnection!.Name.ShouldBe("New Connection");
        capturedConnection.ProviderId.ShouldBe("openai");
        capturedConnection.Settings.ShouldBe(settings);
        capturedConnection.IsActive.ShouldBeFalse();
    }

    #endregion
}
