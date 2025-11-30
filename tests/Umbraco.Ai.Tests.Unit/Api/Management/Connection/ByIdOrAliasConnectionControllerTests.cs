using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Connection.Controllers;
using Umbraco.Ai.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Connection;

public class ByIdOrAliasConnectionControllerTests
{
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly ByIdOrAliasConnectionController _controller;

    public ByIdOrAliasConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new ByIdOrAliasConnectionController(_connectionServiceMock.Object, _mapperMock.Object);
    }

    #region GetConnectionByIdOrAlias - By ID

    [Fact]
    public async Task GetConnectionByIdOrAlias_WithExistingId_ReturnsConnection()
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
        var result = await _controller.GetConnectionByIdOrAlias(new IdOrAlias(connectionId));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var model = okResult.Value.ShouldBeOfType<ConnectionResponseModel>();
        model.Id.ShouldBe(connectionId);
        model.Name.ShouldBe("Test Connection");
    }

    [Fact]
    public async Task GetConnectionByIdOrAlias_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        // Act
        var result = await _controller.GetConnectionByIdOrAlias(new IdOrAlias(connectionId));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task GetConnectionByIdOrAlias_WithId_CallsServiceWithCorrectId()
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
        await _controller.GetConnectionByIdOrAlias(new IdOrAlias(connectionId));

        // Assert
        _connectionServiceMock.Verify(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetConnectionByIdOrAlias - By Alias

    [Fact]
    public async Task GetConnectionByIdOrAlias_WithExistingAlias_ReturnsConnection()
    {
        // Arrange
        var alias = "my-connection";
        var connection = new AiConnectionBuilder()
            .WithAlias(alias)
            .WithName("Test Connection")
            .WithProviderId("openai")
            .Build();

        var responseModel = new ConnectionResponseModel
        {
            Id = connection.Id,
            Alias = connection.Alias,
            Name = connection.Name,
            ProviderId = connection.ProviderId,
            Settings = connection.Settings,
            IsActive = connection.IsActive
        };

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _mapperMock
            .Setup(x => x.Map<ConnectionResponseModel>(connection))
            .Returns(responseModel);

        // Act
        var result = await _controller.GetConnectionByIdOrAlias(new IdOrAlias(alias));

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var model = okResult.Value.ShouldBeOfType<ConnectionResponseModel>();
        model.Alias.ShouldBe(alias);
        model.Name.ShouldBe("Test Connection");
    }

    [Fact]
    public async Task GetConnectionByIdOrAlias_WithNonExistingAlias_Returns404NotFound()
    {
        // Arrange
        var alias = "non-existing";

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        // Act
        var result = await _controller.GetConnectionByIdOrAlias(new IdOrAlias(alias));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    [Fact]
    public async Task GetConnectionByIdOrAlias_WithAlias_CallsServiceWithCorrectAlias()
    {
        // Arrange
        var alias = "my-connection";
        var connection = new AiConnectionBuilder().WithAlias(alias).Build();

        _connectionServiceMock
            .Setup(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _mapperMock
            .Setup(x => x.Map<ConnectionResponseModel>(It.IsAny<AiConnection>()))
            .Returns(new ConnectionResponseModel());

        // Act
        await _controller.GetConnectionByIdOrAlias(new IdOrAlias(alias));

        // Assert
        _connectionServiceMock.Verify(x => x.GetConnectionByAliasAsync(alias, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
