using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
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

    #region Test

    [Fact]
    public async Task Test_WithValidConnection_ReturnsSuccessResult()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Test(connectionId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var testResult = okResult.Value.ShouldBeOfType<ConnectionTestResultModel>();
        testResult.Success.ShouldBeTrue();
        testResult.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task Test_WithFailingConnection_ReturnsFailureResult()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Test(connectionId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var testResult = okResult.Value.ShouldBeOfType<ConnectionTestResultModel>();
        testResult.Success.ShouldBeFalse();
        testResult.ErrorMessage.ShouldBe("Connection test failed");
    }

    [Fact]
    public async Task Test_WithNonExistingConnection_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Connection with ID '{connectionId}' not found"));

        // Act
        var result = await _controller.Test(connectionId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task Test_WithProviderException_ReturnsFailureWithMessage()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var errorMessage = "Authentication failed: Invalid API key";

        _connectionServiceMock
            .Setup(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Test(connectionId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var testResult = okResult.Value.ShouldBeOfType<ConnectionTestResultModel>();
        testResult.Success.ShouldBeFalse();
        testResult.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public async Task Test_CallsServiceWithCorrectId()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _controller.Test(connectionId);

        // Assert
        _connectionServiceMock.Verify(x => x.TestConnectionAsync(connectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
