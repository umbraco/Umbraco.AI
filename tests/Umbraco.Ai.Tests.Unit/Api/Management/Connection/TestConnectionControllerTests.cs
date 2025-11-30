using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Connection.Controllers;
using Umbraco.Ai.Web.Api.Management.Connection.Models;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Connection;

public class TestConnectionControllerTests
{
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly TestConnectionController _controller;

    public TestConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
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

        // TryGetConnectionIdAsync returns the ID directly (no lookup for IDs)
        // TestConnectionAsync throws when connection doesn't exist
        _connectionServiceMock
            .Setup(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Connection with ID '{connectionId}' not found"));

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
        var connection = new AiConnectionBuilder().WithId(connectionId).WithAlias(alias).Build();

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
            .ReturnsAsync((AiConnection?)null);

        // Act
        var result = await _controller.TestConnection(new IdOrAlias(alias));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    #endregion
}
