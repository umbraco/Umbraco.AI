using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.AI.Web.Api.Management.Provider.Controllers;
using Umbraco.AI.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Provider;

public class AllProviderControllerTests
{
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly Mock<IAIExperimentalFeatures> _experimentalFeaturesMock;
    private List<IAIProvider> _providers = new();

    public AllProviderControllerTests()
    {
        _mapperMock = new Mock<IUmbracoMapper>();

        // These tests are about mapper delegation, not capability filtering (see
        // AllProviderControllerExperimentalTests for that) — every capability reports enabled.
        _experimentalFeaturesMock = new Mock<IAIExperimentalFeatures>();
        _experimentalFeaturesMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<AICapability>())).Returns(true);
    }

    private AllProviderController CreateController()
    {
        var collection = new AIProviderCollection(() => _providers);
        return new AllProviderController(collection, _mapperMock.Object, _experimentalFeaturesMock.Object);
    }

    #region GetAllProviders

    [Fact]
    public async Task GetAllProviders_ReturnsAllProviders()
    {
        // Arrange
        _providers = new List<IAIProvider>
        {
            new FakeAIProvider("openai", "OpenAI").WithChatCapability(),
            new FakeAIProvider("anthropic", "Anthropic").WithChatCapability()
        };

        var responseModels = _providers.Select(p => new ProviderItemResponseModel
        {
            Id = p.Id,
            Name = p.Name
        }).ToList();

        _mapperMock
            .Setup(x => x.MapEnumerable<IAIProvider, ProviderItemResponseModel>(It.IsAny<IEnumerable<IAIProvider>>()))
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
        _providers = new List<IAIProvider>();

        _mapperMock
            .Setup(x => x.MapEnumerable<IAIProvider, ProviderItemResponseModel>(It.IsAny<IEnumerable<IAIProvider>>()))
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
        _providers = new List<IAIProvider>
        {
            new FakeAIProvider("test", "Test Provider").WithChatCapability()
        };

        _mapperMock
            .Setup(x => x.MapEnumerable<IAIProvider, ProviderItemResponseModel>(It.IsAny<IEnumerable<IAIProvider>>()))
            .Returns(new List<ProviderItemResponseModel>());

        var controller = CreateController();

        // Act
        await controller.GetAllProviders();

        // Assert
        _mapperMock.Verify(x => x.MapEnumerable<IAIProvider, ProviderItemResponseModel>(
            It.Is<IEnumerable<IAIProvider>>(p => p.Count() == 1 && p.First().Id == "test")), Times.Once);
    }

    #endregion
}
