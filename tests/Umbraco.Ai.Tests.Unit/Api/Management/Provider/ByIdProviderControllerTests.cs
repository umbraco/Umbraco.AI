using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Tests.Common.Fakes;
using Umbraco.Ai.Web.Api.Management.Provider.Controllers;
using Umbraco.Ai.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Provider;

public class ByIdProviderControllerTests
{
    private readonly Mock<IAiRegistry> _registryMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly ByIdProviderController _controller;

    public ByIdProviderControllerTests()
    {
        _registryMock = new Mock<IAiRegistry>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new ByIdProviderController(_registryMock.Object, _mapperMock.Object);
    }

    #region GetProviderById

    [Fact]
    public async Task GetProviderById_WithExistingProvider_ReturnsProvider()
    {
        // Arrange
        var providerId = "openai";
        var provider = new FakeAiProvider(providerId, "OpenAI");

        var responseModel = new ProviderResponseModel
        {
            Id = provider.Id,
            Name = provider.Name,
            SettingDefinitions = new List<SettingDefinitionModel>
            {
                new() { Key = "ApiKey", Label = "API Key", IsRequired = true }
            }
        };

        _registryMock
            .Setup(x => x.GetProvider(providerId))
            .Returns(provider);

        _mapperMock
            .Setup(x => x.Map<ProviderResponseModel>(provider))
            .Returns(responseModel);

        // Act
        var result = await _controller.GetProviderById(providerId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var model = okResult.Value.ShouldBeOfType<ProviderResponseModel>();
        model.Id.ShouldBe(providerId);
        model.Name.ShouldBe("OpenAI");
        model.SettingDefinitions.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetProviderById_WithNonExistingProvider_Returns404NotFound()
    {
        // Arrange
        var providerId = "unknown-provider";

        _registryMock
            .Setup(x => x.GetProvider(providerId))
            .Returns((IAiProvider?)null);

        // Act
        var result = await _controller.GetProviderById(providerId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Provider not found");
    }

    [Fact]
    public async Task GetProviderById_CallsRegistryWithCorrectId()
    {
        // Arrange
        var providerId = "openai";
        var provider = new FakeAiProvider(providerId, "OpenAI");

        _registryMock
            .Setup(x => x.GetProvider(providerId))
            .Returns(provider);

        _mapperMock
            .Setup(x => x.Map<ProviderResponseModel>(It.IsAny<IAiProvider>()))
            .Returns(new ProviderResponseModel());

        // Act
        await _controller.GetProviderById(providerId);

        // Assert
        _registryMock.Verify(x => x.GetProvider(providerId), Times.Once);
    }

    #endregion
}
