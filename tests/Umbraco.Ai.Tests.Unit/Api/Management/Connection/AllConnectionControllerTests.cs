using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Management.Connection.Controllers;
using Umbraco.Ai.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Connection;

public class AllConnectionControllerTests
{
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly AllConnectionController _controller;

    public AllConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new AllConnectionController(_connectionServiceMock.Object, _mapperMock.Object);
    }

    #region GetAllConnections

    [Fact]
    public async Task GetAllConnections_WithNoFilter_ReturnsAllConnections()
    {
        // Arrange
        var connections = new List<AiConnection>
        {
            new AiConnectionBuilder().WithName("Connection 1").Build(),
            new AiConnectionBuilder().WithName("Connection 2").Build()
        };

        var responseModels = connections.Select(c => new ConnectionItemResponseModel
        {
            Id = c.Id,
            Alias = c.Alias,
            Name = c.Name,
            ProviderId = c.ProviderId,
            IsActive = c.IsActive
        }).ToList();

        _connectionServiceMock
            .Setup(x => x.GetConnectionsPagedAsync(null, null, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((connections.AsEnumerable(), connections.Count));

        _mapperMock
            .Setup(x => x.MapEnumerable<AiConnection, ConnectionItemResponseModel>(It.IsAny<IEnumerable<AiConnection>>()))
            .Returns(responseModels);

        // Act
        var result = await _controller.GetConnections();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ConnectionItemResponseModel>>();
        viewModel.Total.ShouldBe(2);
        viewModel.Items.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetAllConnections_WithProviderFilter_ReturnsFilteredConnections()
    {
        // Arrange
        var connections = new List<AiConnection>
        {
            new AiConnectionBuilder().WithName("OpenAI 1").WithProviderId("openai").Build()
        };

        var responseModels = connections.Select(c => new ConnectionItemResponseModel
        {
            Id = c.Id,
            Alias = c.Alias,
            Name = c.Name,
            ProviderId = c.ProviderId,
            IsActive = c.IsActive
        }).ToList();

        _connectionServiceMock
            .Setup(x => x.GetConnectionsPagedAsync(null, "openai", 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((connections.AsEnumerable(), connections.Count));

        _mapperMock
            .Setup(x => x.MapEnumerable<AiConnection, ConnectionItemResponseModel>(It.IsAny<IEnumerable<AiConnection>>()))
            .Returns(responseModels);

        // Act
        var result = await _controller.GetConnections(providerId: "openai");

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ConnectionItemResponseModel>>();
        viewModel.Total.ShouldBe(1);
    }

    [Fact]
    public async Task GetAllConnections_WithEmptyList_ReturnsEmptyPagedViewModel()
    {
        // Arrange
        var connections = new List<AiConnection>();

        _connectionServiceMock
            .Setup(x => x.GetConnectionsPagedAsync(null, null, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((connections.AsEnumerable(), 0));

        _mapperMock
            .Setup(x => x.MapEnumerable<AiConnection, ConnectionItemResponseModel>(It.IsAny<IEnumerable<AiConnection>>()))
            .Returns(new List<ConnectionItemResponseModel>());

        // Act
        var result = await _controller.GetConnections();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ConnectionItemResponseModel>>();
        viewModel.Total.ShouldBe(0);
        viewModel.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllConnections_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var allConnections = Enumerable.Range(1, 10)
            .Select(i => new AiConnectionBuilder().WithName($"Connection {i}").Build())
            .ToList();

        // The service returns the paginated subset but the total count of all items
        var pagedConnections = allConnections.Skip(2).Take(3).ToList();

        _connectionServiceMock
            .Setup(x => x.GetConnectionsPagedAsync(null, null, 2, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((pagedConnections.AsEnumerable(), allConnections.Count));

        _mapperMock
            .Setup(x => x.MapEnumerable<AiConnection, ConnectionItemResponseModel>(It.IsAny<IEnumerable<AiConnection>>()))
            .Returns((IEnumerable<AiConnection> items) => items.Select(c => new ConnectionItemResponseModel
            {
                Id = c.Id,
                Alias = c.Alias,
                Name = c.Name,
                ProviderId = c.ProviderId,
                IsActive = c.IsActive
            }).ToList());

        // Act
        var result = await _controller.GetConnections(skip: 2, take: 3);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ConnectionItemResponseModel>>();
        viewModel.Total.ShouldBe(10); // Total count before pagination
        viewModel.Items.Count().ShouldBe(3); // Only 3 items returned due to take
    }

    #endregion
}
