using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Connection.Controllers;
using Umbraco.AI.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Connection;

public class UpdateConnectionControllerTests
{
    private readonly Mock<IAIConnectionService> _connectionServiceMock;
    private readonly Mock<IUmbracoMapper> _umbracoMapperMock;
    private readonly UpdateConnectionController _controller;

    public UpdateConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAIConnectionService>();
        _umbracoMapperMock = new Mock<IUmbracoMapper>();

        // Setup mapper to simulate Map(source, target) behavior
        _umbracoMapperMock
            .Setup(m => m.Map(It.IsAny<UpdateConnectionRequestModel>(), It.IsAny<AIConnection>()))
            .Returns((UpdateConnectionRequestModel request, AIConnection existing) =>
            {
                // Simulate mapping: update mutable properties, preserve init-only properties
                existing.Name = request.Name;
                existing.Settings = request.Settings;
                existing.IsActive = request.IsActive;
                return existing;
            });

        _controller = new UpdateConnectionController(_connectionServiceMock.Object, _umbracoMapperMock.Object);
    }

    #region UpdateConnection - By ID

    [Fact]
    public async Task UpdateConnection_WithExistingId_ReturnsOk()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var existingConnection = new AIConnectionBuilder()
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
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AIConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection conn, CancellationToken _) => conn);

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
            .ReturnsAsync((AIConnection?)null);

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
        var existingConnection = new AIConnectionBuilder()
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
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AIConnection>(), It.IsAny<CancellationToken>()))
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
        var existingConnection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("openai")
            .Build();

        var requestModel = new UpdateConnectionRequestModel
        {
            Alias = "updated-connection",
            Name = "Updated Name",
            IsActive = true
        };

        AIConnection? capturedConnection = null;
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConnection);

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AIConnection>(), It.IsAny<CancellationToken>()))
            .Callback<AIConnection, CancellationToken>((conn, _) => capturedConnection = conn)
            .ReturnsAsync((AIConnection conn, CancellationToken _) => conn);

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
        var existingConnection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithDateCreated(dateCreated)
            .Build();

        var requestModel = new UpdateConnectionRequestModel
        {
            Alias = "updated-connection",
            Name = "Updated Name",
            IsActive = true
        };

        AIConnection? capturedConnection = null;
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConnection);

        _connectionServiceMock
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AIConnection>(), It.IsAny<CancellationToken>()))
            .Callback<AIConnection, CancellationToken>((conn, _) => capturedConnection = conn)
            .ReturnsAsync((AIConnection conn, CancellationToken _) => conn);

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
        var existingConnection = new AIConnectionBuilder()
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
            .Setup(x => x.SaveConnectionAsync(It.IsAny<AIConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection conn, CancellationToken _) => conn);

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
            .ReturnsAsync((AIConnection?)null);

        // Act
        var result = await _controller.UpdateConnection(new IdOrAlias(alias), requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Connection not found");
    }

    #endregion
}
