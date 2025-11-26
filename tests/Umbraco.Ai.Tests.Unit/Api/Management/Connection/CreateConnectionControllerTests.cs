using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Management.Connection.Controllers;
using Umbraco.Ai.Web.Api.Management.Connection.Models;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Connection;

public class CreateConnectionControllerTests
{
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly CreateConnectionController _controller;

    public CreateConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _controller = new CreateConnectionController(_connectionServiceMock.Object);
    }

    #region Create

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedWithId()
    {
        // Arrange
        var requestModel = new CreateConnectionRequestModel
        {
            Name = "New Connection",
            ProviderId = "openai",
            Settings = new { ApiKey = "test-key" },
            IsActive = true
        };

        var createdId = Guid.NewGuid();
        var createdConnection = new AiConnectionBuilder()
            .WithId(createdId)
            .WithName("New Connection")
            .WithProviderId("openai")
            .Build();

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AiConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdConnection);

        // Act
        var result = await _controller.Create(requestModel);

        // Assert
        var createdResult = result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.ActionName.ShouldBe(nameof(ByIdConnectionController.ById));
        createdResult.Value.ShouldBe(createdId.ToString());
    }

    [Fact]
    public async Task Create_WithProviderNotFound_Returns404NotFound()
    {
        // Arrange
        var requestModel = new CreateConnectionRequestModel
        {
            Name = "New Connection",
            ProviderId = "unknown-provider",
            IsActive = true
        };

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AiConnection>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider 'unknown-provider' not found in registry"));

        // Act
        var result = await _controller.Create(requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Provider not found");
    }

    [Fact]
    public async Task Create_WithInvalidSettings_Returns400BadRequest()
    {
        // Arrange
        var requestModel = new CreateConnectionRequestModel
        {
            Name = "New Connection",
            ProviderId = "openai",
            Settings = new { InvalidKey = "value" },
            IsActive = true
        };

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AiConnection>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Settings validation failed: API Key is required"));

        // Act
        var result = await _controller.Create(requestModel);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Invalid settings");
    }

    [Fact]
    public async Task Create_SetsConnectionIdToEmpty()
    {
        // Arrange
        var requestModel = new CreateConnectionRequestModel
        {
            Name = "New Connection",
            ProviderId = "openai",
            IsActive = true
        };

        AiConnection? capturedConnection = null;
        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AiConnection>(), It.IsAny<CancellationToken>()))
            .Callback<AiConnection, CancellationToken>((conn, _) => capturedConnection = conn)
            .ReturnsAsync((AiConnection conn, CancellationToken _) => conn);

        // Act
        await _controller.Create(requestModel);

        // Assert - Controller should pass Guid.Empty, service generates the ID
        capturedConnection.ShouldNotBeNull();
        capturedConnection!.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task Create_MapsRequestModelToConnection()
    {
        // Arrange
        var settings = new { ApiKey = "test-key" };
        var requestModel = new CreateConnectionRequestModel
        {
            Name = "New Connection",
            ProviderId = "openai",
            Settings = settings,
            IsActive = false
        };

        AiConnection? capturedConnection = null;
        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AiConnection>(), It.IsAny<CancellationToken>()))
            .Callback<AiConnection, CancellationToken>((conn, _) => capturedConnection = conn)
            .ReturnsAsync((AiConnection conn, CancellationToken _) => conn);

        // Act
        await _controller.Create(requestModel);

        // Assert
        capturedConnection.ShouldNotBeNull();
        capturedConnection!.Name.ShouldBe("New Connection");
        capturedConnection.ProviderId.ShouldBe("openai");
        capturedConnection.Settings.ShouldBe(settings);
        capturedConnection.IsActive.ShouldBeFalse();
    }

    #endregion
}
