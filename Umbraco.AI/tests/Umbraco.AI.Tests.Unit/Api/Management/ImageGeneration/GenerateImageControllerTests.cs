#pragma warning disable MEAI001 // Image generation types are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Tests the experimental image-generation controller

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.ImageGeneration;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.ImageGeneration.Controllers;
using Umbraco.AI.Web.Api.Management.ImageGeneration.Models;

namespace Umbraco.AI.Tests.Unit.Api.Management.ImageGeneration;

public class GenerateImageControllerTests
{
    private readonly Mock<IAIImageGenerationService> _serviceMock = new();
    private readonly Mock<IAIProfileService> _profileServiceMock = new();
    private readonly Mock<IAIExperimentalFeatures> _experimentalFeaturesMock = new();

    private GenerateImageController CreateController(bool enabled)
    {
        _experimentalFeaturesMock
            .Setup(x => x.IsCapabilityEnabled(AICapability.ImageGeneration))
            .Returns(enabled);

        return new GenerateImageController(
            _serviceMock.Object,
            _profileServiceMock.Object,
            _experimentalFeaturesMock.Object);
    }

    [Fact]
    public async Task Generate_WhenExperimentalFeatureDisabled_Returns404()
    {
        var controller = CreateController(enabled: false);

        var result = await controller.Generate(new GenerateImageRequestModel { Prompt = "a cat" });

        result.ShouldBeOfType<NotFoundResult>();
        _serviceMock.Verify(
            x => x.GenerateImagesAsync(It.IsAny<Action<AIImageGenerationBuilder>>(), It.IsAny<string>(), It.IsAny<IEnumerable<AIContent>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Generate_WhenEnabled_ReturnsOkWithImages()
    {
        var controller = CreateController(enabled: true);

        var response = new ImageGenerationResponse([new DataContent(new byte[] { 1, 2, 3 }, "image/png")])
        {
            Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 0, TotalTokenCount = 10 },
        };

        _serviceMock
            .Setup(x => x.GenerateImagesAsync(
                It.IsAny<Action<AIImageGenerationBuilder>>(),
                "a cat",
                It.IsAny<IEnumerable<AIContent>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.Generate(new GenerateImageRequestModel { Prompt = "a cat" });

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var model = ok.Value.ShouldBeOfType<GenerateImageResponseModel>();
        model.Images.Count.ShouldBe(1);
        model.Images[0].MediaType.ShouldBe("image/png");
        model.Images[0].Data.ShouldNotBeNull();
        model.Usage.ShouldNotBeNull();
        model.Usage!.TotalTokens.ShouldBe(10);
    }

    [Fact]
    public async Task Generate_WithEmptyPrompt_Returns400()
    {
        var controller = CreateController(enabled: true);

        var result = await controller.Generate(new GenerateImageRequestModel { Prompt = "  " });

        result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
