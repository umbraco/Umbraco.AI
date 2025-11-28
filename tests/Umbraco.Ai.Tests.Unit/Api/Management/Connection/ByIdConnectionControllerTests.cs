using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Management.Connection.Controllers;
using Umbraco.Ai.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Connection;

public class ByIdConnectionControllerTests
{
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly ByIdConnectionController _controller;

    public ByIdConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new ByIdConnectionController(_connectionServiceMock.Object, _mapperMock.Object);
    }

    #region GetConnectionById

    [Fact]
    public async Task GetConnectionById_WithExistingId_ReturnsConnection()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithName("Test Connection")
            .WithProviderId("openai")
            .Build();

        var responseModel = new ConnectionResponseModel
        {
            Id = connection.Id,
            Name = connection.Name,
            ProviderId = connection.ProviderId,
            Settings = connection.Settings,
            IsActive = connection.IsActive
        };

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _mapperMock
            .Setup(x => x.Map<ConnectionResponseModel>(connection))
            .Returns(responseModel);

        // Act
        var result = await _controller.GetConnectionById(connectionId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var model = okResult.Value.ShouldBeOfType<ConnectionResponseModel>();
        model.Id.ShouldBe(connectionId);
        model.Name.ShouldBe("Test Connection");
    }

    [Fact]
    public async Task GetConnectionById_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        // Act
        var result = await _controller.GetConnectionById(connectionId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task GetConnectionById_CallsServiceWithCorrectId()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder().WithId(connectionId).Build();

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _mapperMock
            .Setup(x => x.Map<ConnectionResponseModel>(It.IsAny<AiConnection>()))
            .Returns(new ConnectionResponseModel());

        // Act
        await _controller.GetConnectionById(connectionId);

        // Assert
        _connectionServiceMock.Verify(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
