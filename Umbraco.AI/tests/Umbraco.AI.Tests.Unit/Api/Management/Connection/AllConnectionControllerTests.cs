using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Web.Api.Management.Connection.Controllers;
using Umbraco.AI.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Connection;

public class AllConnectionControllerTests
{
    private readonly Mock<IAIConnectionService> _connectionServiceMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly AllConnectionController _controller;

    public AllConnectionControllerTests()
    {
        _connectionServiceMock = new Mock<IAIConnectionService>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new AllConnectionController(_connectionServiceMock.Object, _mapperMock.Object);
    }

    #region GetAllConnections

    [Fact]
    public async Task GetAllConnections_WithNoFilter_ReturnsAllConnections()
    {
        // Arrange
        var connections = new List<AIConnection>
        {
            new AIConnectionBuilder().WithName("Connection 1").Build(),
            new AIConnectionBuilder().WithName("Connection 2").Build()
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
            .Setup(x => x.MapEnumerable<AIConnection, ConnectionItemResponseModel>(It.IsAny<IEnumerable<AIConnection>>()))
            .Returns(responseModels);

        // Act
        var result = await _controller.GetAllConnections();

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
        var connections = new List<AIConnection>
        {
            new AIConnectionBuilder().WithName("OpenAI 1").WithProviderId("openai").Build()
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
            .Setup(x => x.MapEnumerable<AIConnection, ConnectionItemResponseModel>(It.IsAny<IEnumerable<AIConnection>>()))
            .Returns(responseModels);

        // Act
        var result = await _controller.GetAllConnections(providerId: "openai");

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ConnectionItemResponseModel>>();
        viewModel.Total.ShouldBe(1);
    }

    [Fact]
    public async Task GetAllConnections_WithEmptyList_ReturnsEmptyPagedViewModel()
    {
        // Arrange
        var connections = new List<AIConnection>();

        _connectionServiceMock
            .Setup(x => x.GetConnectionsPagedAsync(null, null, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((connections.AsEnumerable(), 0));

        _mapperMock
            .Setup(x => x.MapEnumerable<AIConnection, ConnectionItemResponseModel>(It.IsAny<IEnumerable<AIConnection>>()))
            .Returns(new List<ConnectionItemResponseModel>());

        // Act
        var result = await _controller.GetAllConnections();

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
            .Select(i => new AIConnectionBuilder().WithName($"Connection {i}").Build())
            .ToList();

        // The service returns the paginated subset but the total count of all items
        var pagedConnections = allConnections.Skip(2).Take(3).ToList();

        _connectionServiceMock
            .Setup(x => x.GetConnectionsPagedAsync(null, null, 2, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((pagedConnections.AsEnumerable(), allConnections.Count));

        _mapperMock
            .Setup(x => x.MapEnumerable<AIConnection, ConnectionItemResponseModel>(It.IsAny<IEnumerable<AIConnection>>()))
            .Returns((IEnumerable<AIConnection> items) => items.Select(c => new ConnectionItemResponseModel
            {
                Id = c.Id,
                Alias = c.Alias,
                Name = c.Name,
                ProviderId = c.ProviderId,
                IsActive = c.IsActive
            }).ToList());

        // Act
        var result = await _controller.GetAllConnections(skip: 2, take: 3);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ConnectionItemResponseModel>>();
        viewModel.Total.ShouldBe(10); // Total count before pagination
        viewModel.Items.Count().ShouldBe(3); // Only 3 items returned due to take
    }

    #endregion
}
