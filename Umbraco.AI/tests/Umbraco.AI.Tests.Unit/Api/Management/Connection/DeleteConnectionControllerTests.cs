using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Connection.Controllers;

namespace Umbraco.AI.Tests.Unit.Api.Management.Connection;

public class DeleteConnectionControllerTests
{
    private readonly Mock<IAIConnectionService> _connectionServiceMock;
    private readonly DeleteConnectionController _controller;

    public DeleteConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAIConnectionService>();
        // Connections are looked up by ID before use; unless a test says otherwise, they exist.
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new AIConnectionBuilder().WithId(id).Build());
        _controller = new DeleteConnectionController(_connectionServiceMock.Object);
    }

    #region DeleteConnection - By ID

    [Fact]
    public async Task DeleteConnection_WithExistingId_ReturnsOk()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.DeleteConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteConnection(new IdOrAlias(connectionId));

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task DeleteConnection_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        // An ID is returned without a lookup, so the controller checks existence explicitly
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection?)null);

        // Act
        var result = await _controller.DeleteConnection(new IdOrAlias(connectionId));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task DeleteConnection_WithConnectionInUse_Returns400BadRequest()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.DeleteConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection is in use by one or more profiles"));

        // Act
        var result = await _controller.DeleteConnection(new IdOrAlias(connectionId));

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection in use");
    }

    [Fact]
    public async Task DeleteConnection_WithId_CallsServiceWithCorrectId()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.DeleteConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.DeleteConnection(new IdOrAlias(connectionId));

        // Assert
        _connectionServiceMock.Verify(x => x.DeleteConnectionAsync(connectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteConnection - By Alias

    [Fact]
    public async Task DeleteConnection_WithExistingAlias_ReturnsOk()
    {
        // Arrange
        var alias = "my-connection";
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder().WithId(connectionId).WithAlias(alias).Build();

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _connectionServiceMock
            .Setup(x => x.DeleteConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteConnection(new IdOrAlias(alias));

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task DeleteConnection_WithNonExistingAlias_Returns404NotFound()
    {
        // Arrange
        var alias = "non-existing";

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection?)null);

        // Act
        var result = await _controller.DeleteConnection(new IdOrAlias(alias));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    #endregion
}
