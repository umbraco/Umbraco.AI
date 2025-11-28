using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Tests.Common.Fakes;
using Umbraco.Ai.Web.Api.Management.Provider.Controllers;
using Umbraco.Ai.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Provider;

public class AllProviderControllerTests
{
    private readonly Mock<IAiRegistry> _registryMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly AllProviderController _controller;

    public AllProviderControllerTests()
    {
        _registryMock = new Mock<IAiRegistry>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new AllProviderController(_registryMock.Object, _mapperMock.Object);
    }

    #region GetAllProviders

    [Fact]
    public async Task GetAllProviders_ReturnsAllProviders()
    {
        // Arrange
        var providers = new List<IAiProvider>
        {
            new FakeAiProvider("openai", "OpenAI"),
            new FakeAiProvider("anthropic", "Anthropic")
        };

        var responseModels = providers.Select(p => new ProviderItemResponseModel
        {
            Id = p.Id,
            Name = p.Name
        }).ToList();

        _registryMock
            .Setup(x => x.Providers)
            .Returns(providers);

        _mapperMock
            .Setup(x => x.MapEnumerable<IAiProvider, ProviderItemResponseModel>(It.IsAny<IEnumerable<IAiProvider>>()))
            .Returns(responseModels);

        // Act
        var result = await _controller.GetAllProviders();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var models = okResult.Value.ShouldBeAssignableTo<IEnumerable<ProviderItemResponseModel>>();
        models!.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetAllProviders_WithNoProviders_ReturnsEmptyList()
    {
        // Arrange
        _registryMock
            .Setup(x => x.Providers)
            .Returns(new List<IAiProvider>());

        _mapperMock
            .Setup(x => x.MapEnumerable<IAiProvider, ProviderItemResponseModel>(It.IsAny<IEnumerable<IAiProvider>>()))
            .Returns(new List<ProviderItemResponseModel>());

        // Act
        var result = await _controller.GetAllProviders();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var models = okResult.Value.ShouldBeAssignableTo<IEnumerable<ProviderItemResponseModel>>();
        models!.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllProviders_CallsMapperWithProvidersFromRegistry()
    {
        // Arrange
        var providers = new List<IAiProvider>
        {
            new FakeAiProvider("test", "Test Provider")
        };

        _registryMock
            .Setup(x => x.Providers)
            .Returns(providers);

        _mapperMock
            .Setup(x => x.MapEnumerable<IAiProvider, ProviderItemResponseModel>(It.IsAny<IEnumerable<IAiProvider>>()))
            .Returns(new List<ProviderItemResponseModel>());

        // Act
        await _controller.GetAllProviders();

        // Assert
        _mapperMock.Verify(x => x.MapEnumerable<IAiProvider, ProviderItemResponseModel>(providers), Times.Once);
    }

    #endregion
}
