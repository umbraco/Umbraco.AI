using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Connection.Controllers;
using Umbraco.Ai.Web.Api.Management.Connection.Models;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Connection;

public class UpdateConnectionControllerTests
{
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly UpdateConnectionController _controller;

    public UpdateConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _controller = new UpdateConnectionController(_connectionServiceMock.Object);
    }

    #region UpdateConnection - By ID

    [Fact]
    public async Task UpdateConnection_WithExistingId_ReturnsOk()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var existingConnection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithName("Old Name")
            .WithProviderId("openai")
            .Build();

        var requestModel = new UpdateConnectionRequestModel
        {
            Alias = "updated-connection",
            Name = "Updated Name",
            Settings = new { ApiKey = "new-key" },
            IsActive = true
        };

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConnection);

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AiConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection conn, CancellationToken _) => conn);

        // Act
        var result = await _controller.UpdateConnection(new IdOrAlias(connectionId), requestModel);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task UpdateConnection_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var requestModel = new UpdateConnectionRequestModel
        {
            Alias = "updated-connection",
            Name = "Updated Name",
            IsActive = true
        };

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        // Act
        var result = await _controller.UpdateConnection(new IdOrAlias(connectionId), requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task UpdateConnection_WithInvalidSettings_Returns400BadRequest()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var existingConnection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("openai")
            .Build();

        var requestModel = new UpdateConnectionRequestModel
        {
            Alias = "updated-connection",
            Name = "Updated Name",
            Settings = new { InvalidKey = "value" },
            IsActive = true
        };

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConnection);

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AiConnection>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Settings validation failed"));

        // Act
        var result = await _controller.UpdateConnection(new IdOrAlias(connectionId), requestModel);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Invalid settings");
    }

    [Fact]
    public async Task UpdateConnection_PreservesProviderIdFromExisting()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var existingConnection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("openai")
            .Build();

        var requestModel = new UpdateConnectionRequestModel
        {
            Alias = "updated-connection",
            Name = "Updated Name",
            IsActive = true
        };

        AiConnection? capturedConnection = null;
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConnection);

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AiConnection>(), It.IsAny<CancellationToken>()))
            .Callback<AiConnection, CancellationToken>((conn, _) => capturedConnection = conn)
            .ReturnsAsync((AiConnection conn, CancellationToken _) => conn);

        // Act
        await _controller.UpdateConnection(new IdOrAlias(connectionId), requestModel);

        // Assert - ProviderId should be preserved from existing connection
        capturedConnection.ShouldNotBeNull();
        capturedConnection!.ProviderId.ShouldBe("openai");
    }

    [Fact]
    public async Task UpdateConnection_PreservesDateCreatedFromExisting()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var dateCreated = DateTime.UtcNow.AddDays(-5);
        var existingConnection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithDateCreated(dateCreated)
            .Build();

        var requestModel = new UpdateConnectionRequestModel
        {
            Alias = "updated-connection",
            Name = "Updated Name",
            IsActive = true
        };

        AiConnection? capturedConnection = null;
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConnection);

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AiConnection>(), It.IsAny<CancellationToken>()))
            .Callback<AiConnection, CancellationToken>((conn, _) => capturedConnection = conn)
            .ReturnsAsync((AiConnection conn, CancellationToken _) => conn);

        // Act
        await _controller.UpdateConnection(new IdOrAlias(connectionId), requestModel);

        // Assert - DateCreated should be preserved from existing connection
        capturedConnection.ShouldNotBeNull();
        capturedConnection!.DateCreated.ShouldBe(dateCreated);
    }

    #endregion

    #region UpdateConnection - By Alias

    [Fact]
    public async Task UpdateConnection_WithExistingAlias_ReturnsOk()
    {
        // Arrange
        var alias = "my-connection";
        var existingConnection = new AiConnectionBuilder()
            .WithAlias(alias)
            .WithName("Old Name")
            .WithProviderId("openai")
            .Build();

        var requestModel = new UpdateConnectionRequestModel
        {
            Alias = "updated-connection",
            Name = "Updated Name",
            Settings = new { ApiKey = "new-key" },
            IsActive = true
        };

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConnection);

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AiConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection conn, CancellationToken _) => conn);

        // Act
        var result = await _controller.UpdateConnection(new IdOrAlias(alias), requestModel);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task UpdateConnection_WithNonExistingAlias_Returns404NotFound()
    {
        // Arrange
        var alias = "non-existing";
        var requestModel = new UpdateConnectionRequestModel
        {
            Alias = "updated-connection",
            Name = "Updated Name",
            IsActive = true
        };

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        // Act
        var result = await _controller.UpdateConnection(new IdOrAlias(alias), requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    #endregion
}
