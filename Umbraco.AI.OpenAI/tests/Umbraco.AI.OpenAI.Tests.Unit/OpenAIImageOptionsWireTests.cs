using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using OpenAI;
using Umbraco.AI.OpenAI.Tests.Unit.Fakes;

#pragma warning disable MEAI001 // IImageGenerator / ImageGenerationOptions are experimental in M.E.AI

namespace Umbraco.AI.OpenAI.Tests.Unit;

/// <summary>
/// Pins down which image-generation options actually reach the OpenAI wire, asserted against a captured
/// request body rather than a recording generator.
/// </summary>
/// <remarks>
/// A recording generator only proves what we handed the adapter, and stays green when the adapter drops a
/// value — which is exactly how the quality and style hints turned out to be silent no-ops. These tests hold
/// both halves of that finding: what the adapter ignores, and what it honours.
/// </remarks>
public class OpenAIImageOptionsWireTests
{
    [Fact]
    public async Task AdditionalProperties_QualityAndStyle_NeverReachTheRequest()
    {
        // The reason OpenAIImageHints exists. M.E.AI has no first-class property for either hint, so they
        // travel as additional properties — which the OpenAI adapter does not read.
        var handler = new CapturingHttpMessageHandler();
        var generator = CreateImageGenerator(handler, "dall-e-3");

        var options = new ImageGenerationOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["quality"] = "hd",
                ["style"] = "vivid",
            },
        };

        await SendAndIgnoreFailureAsync(generator, options);

        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldNotContain("quality");
        body.ShouldNotContain("style");
    }

    [Fact]
    public async Task RawRepresentation_QualityAndStyle_ReachTheRequestWithoutLosingAdapterOptions()
    {
        var handler = new CapturingHttpMessageHandler();
        var generator = CreateImageGenerator(handler, "dall-e-3");

        var options = new ImageGenerationOptions
        {
            Count = 1,
            MediaType = "image/png",
            RawRepresentationFactory = _ => new global::OpenAI.Images.ImageGenerationOptions
            {
                Quality = new global::OpenAI.Images.GeneratedImageQuality("hd"),
                Style = new global::OpenAI.Images.GeneratedImageStyle("vivid"),
            },
        };

        await SendAndIgnoreFailureAsync(generator, options);

        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"quality\":\"hd\"");
        body.ShouldContain("\"style\":\"vivid\"");
        // Load-bearing: the adapter must still fill everything the representation left empty, or translating
        // the hints this way would silently drop the rest of the request.
        body.ShouldContain("\"model\":\"dall-e-3\"");
        body.ShouldContain("\"n\":1");
        body.ShouldContain("\"output_format\":\"png\"");
    }

    [Fact]
    public async Task HintGenerator_TranslatesAdditionalProperties_SoTheHintsReachTheRequest()
    {
        // End to end through the decorator the capability installs: hints in as additional properties, out
        // on the wire.
        var handler = new CapturingHttpMessageHandler();
        var generator = new OpenAIImageHintGenerator(CreateImageGenerator(handler, "dall-e-3"), "dall-e-3", logger: null);

        var options = new ImageGenerationOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["quality"] = "hd",
                ["style"] = "vivid",
            },
        };

        await SendAndIgnoreFailureAsync(generator, options);

        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"quality\":\"hd\"");
        body.ShouldContain("\"style\":\"vivid\"");
    }

    [Fact]
    public async Task HintGenerator_GptImageQualityVocabulary_ReachesTheRequestVerbatim()
    {
        // gpt-image accepts low/medium/high where DALL·E 3 accepts standard/hd, and the hint is passed
        // through rather than mapped, so both vocabularies work without a per-model table.
        var handler = new CapturingHttpMessageHandler();
        var generator = new OpenAIImageHintGenerator(CreateImageGenerator(handler, "gpt-image-1"), "gpt-image-1", logger: null);

        var options = new ImageGenerationOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary { ["quality"] = "high" },
        };

        await SendAndIgnoreFailureAsync(generator, options);

        handler.RequestBodies.ShouldHaveSingleItem().ShouldContain("\"quality\":\"high\"");
    }

    [Fact]
    public async Task HintGenerator_UnrecognisedQuality_IsSkippedRatherThanSent()
    {
        // Sending it would fail the request outright; a profile saved with a typo should still generate.
        var handler = new CapturingHttpMessageHandler();
        var generator = new OpenAIImageHintGenerator(CreateImageGenerator(handler, "gpt-image-1"), "gpt-image-1", logger: null);

        var options = new ImageGenerationOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary { ["quality"] = "ultra" },
        };

        await SendAndIgnoreFailureAsync(generator, options);

        handler.RequestBodies.ShouldHaveSingleItem().ShouldNotContain("quality");
    }

    [Fact]
    public async Task HintGenerator_CallerSuppliedRawRepresentation_Wins()
    {
        // A caller that built the SDK options itself is more specific than a profile-level hint, and
        // overwriting its factory would drop whatever else it set.
        var handler = new CapturingHttpMessageHandler();
        var generator = new OpenAIImageHintGenerator(CreateImageGenerator(handler, "dall-e-3"), "dall-e-3", logger: null);

        var options = new ImageGenerationOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary { ["quality"] = "hd" },
            RawRepresentationFactory = _ => new global::OpenAI.Images.ImageGenerationOptions
            {
                Quality = new global::OpenAI.Images.GeneratedImageQuality("standard"),
            },
        };

        await SendAndIgnoreFailureAsync(generator, options);

        handler.RequestBodies.ShouldHaveSingleItem().ShouldContain("\"quality\":\"standard\"");
    }

    [Fact]
    public async Task HintGenerator_NoHints_LeavesTheOptionsAlone()
    {
        var handler = new CapturingHttpMessageHandler();
        var generator = new OpenAIImageHintGenerator(CreateImageGenerator(handler, "dall-e-3"), "dall-e-3", logger: null);

        await SendAndIgnoreFailureAsync(generator, new ImageGenerationOptions { MediaType = "image/png" });

        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"output_format\":\"png\"");
        body.ShouldNotContain("quality");
    }

    [Theory]
    [InlineData("dall-e-3", "hd", true)]
    [InlineData("dall-e-3", "high", false)]
    [InlineData("gpt-image-1", "high", true)]
    [InlineData("gpt-image-1", "hd", false)]
    [InlineData("dall-e-2", "standard", true)]
    [InlineData("dall-e-2", "hd", false)]
    public async Task CapabilitySettings_Quality_IsGatedByModelVocabulary(
        string modelId,
        string quality,
        bool expectedOnTheWire)
    {
        // One field, two vocabularies: DALL·E takes standard/hd, gpt-image takes low/medium/high/auto. A
        // value the selected model does not know is dropped rather than sent, because the API rejects the
        // whole request for it and a profile carried across models should degrade instead of failing.
        var handler = new CapturingHttpMessageHandler();
        var options = new ImageGenerationOptions
        {
            RawRepresentationFactory = OpenAIImageHints.CreateRawFactory(quality, style: null, modelId, logger: null),
        };

        await SendAndIgnoreFailureAsync(CreateImageGenerator(handler, modelId), options);

        var body = handler.RequestBodies.ShouldHaveSingleItem();
        if (expectedOnTheWire)
        {
            body.ShouldContain($"\"quality\":\"{quality}\"");
        }
        else
        {
            body.ShouldNotContain("quality");
        }
    }

    [Fact]
    public void CapabilitySettings_UnknownModel_PassesQualityThrough()
    {
        // We cannot know what an unrecognised model accepts, so a deliberate value goes out rather than
        // being dropped on a guess.
        OpenAIImageHints
            .CreateRawFactory("hd", style: null, "some-future-image-model", logger: null)
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task CapabilitySettings_Style_OnlyReachesDallE3()
    {
        var handler = new CapturingHttpMessageHandler();
        var options = new ImageGenerationOptions
        {
            RawRepresentationFactory = OpenAIImageHints.CreateRawFactory(
                quality: null,
                style: "vivid",
                "gpt-image-1",
                logger: null),
        };

        await SendAndIgnoreFailureAsync(CreateImageGenerator(handler, "gpt-image-1"), options);

        // The editor hides the field on gpt-image via GetSettingsSupport, but a profile saved before the
        // model changed still reaches here, so the gate is enforced rather than assumed.
        handler.RequestBodies.ShouldHaveSingleItem().ShouldNotContain("style");
    }

    private static IImageGenerator CreateImageGenerator(CapturingHttpMessageHandler handler, string modelId)
        => new OpenAIClient(
                new ApiKeyCredential("test-key"),
                new OpenAIClientOptions
                {
                    Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
                    RetryPolicy = new ClientRetryPolicy(0),
                })
            .GetImageClient(modelId)
            .AsIImageGenerator();

    private static async Task SendAndIgnoreFailureAsync(IImageGenerator generator, ImageGenerationOptions options)
    {
        try
        {
            await generator.GenerateAsync(new ImageGenerationRequest { Prompt = "a cat" }, options);
        }
        catch (Exception)
        {
            // The capturing handler always fails the request; only the captured body matters here.
        }
    }
}
