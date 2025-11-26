using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Web.Api.Management.Connection.Controllers;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Connection;

public class DeleteConnectionControllerTests
{
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly DeleteConnectionController _controller;

    public DeleteConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _controller = new DeleteConnectionController(_connectionServiceMock.Object);
    }

    #region Delete

    [Fact]
    public async Task Delete_WithExistingConnection_ReturnsOk()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.DeleteConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(connectionId);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistingConnection_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.DeleteConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Connection with ID '{connectionId}' not found"));

        // Act
        var result = await _controller.Delete(connectionId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task Delete_WithConnectionInUse_Returns400BadRequest()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.DeleteConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection is in use by one or more profiles"));

        // Act
        var result = await _controller.Delete(connectionId);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection in use");
    }

    [Fact]
    public async Task Delete_CallsServiceWithCorrectId()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.DeleteConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.Delete(connectionId);

        // Assert
        _connectionServiceMock.Verify(x => x.DeleteConnectionAsync(connectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
