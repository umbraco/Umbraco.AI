using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.AI.Web.Api.Management.Provider.Controllers;
using Umbraco.AI.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Provider;

public class ByIdProviderControllerTests
{
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private List<IAIProvider> _providers = new();

    public ByIdProviderControllerTests()
    {
        _mapperMock = new Mock<IUmbracoMapper>();
    }

    private ByIdProviderController CreateController()
    {
        var collection = new AIProviderCollection(() => _providers);
        return new ByIdProviderController(collection, _mapperMock.Object);
    }

    #region GetProviderById

    [Fact]
    public async Task GetProviderById_WithExistingProvider_ReturnsProvider()
    {
        // Arrange
        var providerId = "openai";
        var provider = new FakeAIProvider(providerId, "OpenAI");
        _providers.Add(provider);

        var responseModel = new ProviderResponseModel
        {
            Id = provider.Id,
            Name = provider.Name,
            SettingsSchema = new EditableModelSchemaModel
            {
                Fields = new List<EditableModelFieldModel>
                {
                    new() { Key = "ApiKey", Label = "API Key", IsRequired = true }
                }
            }
        };

        _mapperMock
            .Setup(x => x.Map<ProviderResponseModel>(provider))
            .Returns(responseModel);

        var controller = CreateController();

        // Act
        var result = await controller.GetProviderById(providerId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var model = okResult.Value.ShouldBeOfType<ProviderResponseModel>();
        model.Id.ShouldBe(providerId);
        model.Name.ShouldBe("OpenAI");
        model.SettingsSchema.ShouldNotBeNull();
        model.SettingsSchema!.Fields.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetProviderById_WithNonExistingProvider_Returns404NotFound()
    {
        // Arrange
        var providerId = "unknown-provider";
        // No providers added

        var controller = CreateController();

        // Act
        var result = await controller.GetProviderById(providerId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Provider not found");
    }

    [Fact]
    public async Task GetProviderById_CallsMapperWithCorrectProvider()
    {
        // Arrange
        var providerId = "openai";
        var provider = new FakeAIProvider(providerId, "OpenAI");
        _providers.Add(provider);

        _mapperMock
            .Setup(x => x.Map<ProviderResponseModel>(It.IsAny<IAIProvider>()))
            .Returns(new ProviderResponseModel());

        var controller = CreateController();

        // Act
        await controller.GetProviderById(providerId);

        // Assert
        _mapperMock.Verify(x => x.Map<ProviderResponseModel>(
            It.Is<IAIProvider>(p => p.Id == providerId)), Times.Once);
    }

    #endregion
}
