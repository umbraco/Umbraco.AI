using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Tests.Common.Fakes;
using Umbraco.Ai.Web.Api.Management.Provider.Controllers;
using Umbraco.Ai.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Provider;

public class AllProviderControllerTests
{
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private List<IAiProvider> _providers = new();

    public AllProviderControllerTests()
    {
        _mapperMock = new Mock<IUmbracoMapper>();
    }

    private AllProviderController CreateController()
    {
        var collection = new AiProviderCollection(() => _providers);
        return new AllProviderController(collection, _mapperMock.Object);
    }

    #region GetAllProviders

    [Fact]
    public async Task GetAllProviders_ReturnsAllProviders()
    {
        // Arrange
        _providers = new List<IAiProvider>
        {
            new FakeAiProvider("openai", "OpenAI"),
            new FakeAiProvider("anthropic", "Anthropic")
        };

        var responseModels = _providers.Select(p => new ProviderItemResponseModel
        {
            Id = p.Id,
            Name = p.Name
        }).ToList();

        _mapperMock
            .Setup(x => x.MapEnumerable<IAiProvider, ProviderItemResponseModel>(It.IsAny<IEnumerable<IAiProvider>>()))
            .Returns(responseModels);

        var controller = CreateController();

        // Act
        var result = await controller.GetAllProviders();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var models = okResult.Value.ShouldBeAssignableTo<IEnumerable<ProviderItemResponseModel>>();
        models!.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetAllProviders_WithNoProviders_ReturnsEmptyList()
    {
        // Arrange
        _providers = new List<IAiProvider>();

        _mapperMock
            .Setup(x => x.MapEnumerable<IAiProvider, ProviderItemResponseModel>(It.IsAny<IEnumerable<IAiProvider>>()))
            .Returns(new List<ProviderItemResponseModel>());

        var controller = CreateController();

        // Act
        var result = await controller.GetAllProviders();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var models = okResult.Value.ShouldBeAssignableTo<IEnumerable<ProviderItemResponseModel>>();
        models!.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllProviders_CallsMapperWithProvidersFromCollection()
    {
        // Arrange
        _providers = new List<IAiProvider>
        {
            new FakeAiProvider("test", "Test Provider")
        };

        _mapperMock
            .Setup(x => x.MapEnumerable<IAiProvider, ProviderItemResponseModel>(It.IsAny<IEnumerable<IAiProvider>>()))
            .Returns(new List<ProviderItemResponseModel>());

        var controller = CreateController();

        // Act
        await controller.GetAllProviders();

        // Assert
        _mapperMock.Verify(x => x.MapEnumerable<IAiProvider, ProviderItemResponseModel>(
            It.Is<IEnumerable<IAiProvider>>(p => p.Count() == 1 && p.First().Id == "test")), Times.Once);
    }

    #endregion
}
