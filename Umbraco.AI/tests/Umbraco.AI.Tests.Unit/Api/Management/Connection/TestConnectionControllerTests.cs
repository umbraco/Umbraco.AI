using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Connection.Controllers;
using Umbraco.AI.Web.Api.Management.Connection.Models;

namespace Umbraco.AI.Tests.Unit.Api.Management.Connection;

public class TestConnectionControllerTests
{
    private readonly Mock<IAIConnectionService> _connectionServiceMock;
    private readonly TestConnectionController _controller;

    public TestConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAIConnectionService>();
        // Connections are looked up by ID before use; unless a test says otherwise, they exist.
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new AIConnectionBuilder().WithId(id).Build());
        _controller = new TestConnectionController(_connectionServiceMock.Object);
    }

    #region TestConnection - By ID

    [Fact]
    public async Task TestConnection_WithValidId_ReturnsSuccessResult()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.TestConnection(new IdOrAlias(connectionId));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var testResult = okResult.Value.ShouldBeOfType<ConnectionTestResultModel>();
        testResult.Success.ShouldBeTrue();
        testResult.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task TestConnection_WithFailingConnection_ReturnsFailureResult()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.TestConnection(new IdOrAlias(connectionId));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var testResult = okResult.Value.ShouldBeOfType<ConnectionTestResultModel>();
        testResult.Success.ShouldBeFalse();
        testResult.ErrorMessage.ShouldBe("Connection test failed");
    }

    [Fact]
    public async Task TestConnection_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        // An ID is returned without a lookup, so the controller checks existence explicitly
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection?)null);

        // Act
        var result = await _controller.TestConnection(new IdOrAlias(connectionId));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task TestConnection_WithProviderException_ReturnsFailureWithMessage()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var errorMessage = "Authentication failed: Invalid API key";

        _connectionServiceMock
            .Setup(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.TestConnection(new IdOrAlias(connectionId));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var testResult = okResult.Value.ShouldBeOfType<ConnectionTestResultModel>();
        testResult.Success.ShouldBeFalse();
        testResult.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public async Task TestConnection_WithId_CallsServiceWithCorrectId()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _controller.TestConnection(new IdOrAlias(connectionId));

        // Assert
        _connectionServiceMock.Verify(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region TestConnection - By Alias

    [Fact]
    public async Task TestConnection_WithValidAlias_ReturnsSuccessResult()
    {
        // Arrange
        var alias = "my-connection";
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder().WithId(connectionId).WithAlias(alias).Build();

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _connectionServiceMock
            .Setup(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.TestConnection(new IdOrAlias(alias));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var testResult = okResult.Value.ShouldBeOfType<ConnectionTestResultModel>();
        testResult.Success.ShouldBeTrue();
        testResult.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task TestConnection_WithNonExistingAlias_Returns404NotFound()
    {
        // Arrange
        var alias = "non-existing";

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection?)null);

        // Act
        var result = await _controller.TestConnection(new IdOrAlias(alias));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    #endregion
}
