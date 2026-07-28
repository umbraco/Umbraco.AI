using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Core.Models;
using Umbraco.AI.OpenAI.Tests.Unit.Fakes;

#pragma warning disable UMBRACOAI_IMAGEGEN // Covers the experimental image capability's declarations

namespace Umbraco.AI.OpenAI.Tests.Unit;

/// <summary>
/// What the capability declares per model, which is what the profile editor renders from. The predicates
/// behind it are covered elsewhere; this pins that both of them reach the declaration, and independently —
/// reasoning effort and temperature are supported by opposite sets of models, so a declaration that read
/// one predicate for both answers would look plausible and be wrong on every model.
/// </summary>
public class OpenAISettingsDeclarationTests
{
    [Theory]
    [InlineData("o3-mini")]
    [InlineData("gpt-5.6")]
    public void GetSettingsSupport_ReasoningModel_DeclaresTemperatureUnsupportedOnly(string modelId)
    {
        var metadata = CreateCapability().GetSettingsSupport(modelId).ToMetadata();

        metadata[AIModelMetadataKeys.ProfileSettingsUnsupported].ShouldBe("temperature");
        metadata.ContainsKey(AIModelMetadataKeys.CapabilitySettingsUnsupported).ShouldBeFalse();
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("chatgpt-4o-latest")]
    public void GetSettingsSupport_SamplingModel_DeclaresReasoningEffortUnsupportedOnly(string modelId)
    {
        var metadata = CreateCapability().GetSettingsSupport(modelId).ToMetadata();

        metadata[AIModelMetadataKeys.CapabilitySettingsUnsupported].ShouldBe("reasoningEffort");
        metadata.ContainsKey(AIModelMetadataKeys.ProfileSettingsUnsupported).ShouldBeFalse();
    }

    [Fact]
    public void GetSettingsSupport_UnknownModel_DeclaresBothUnsupported()
    {
        // Both predicates are allow-lists that fail safe, so a model released after this package ships
        // hides each setting rather than offering one the model may reject.
        var metadata = CreateCapability().GetSettingsSupport("gpt-6").ToMetadata();

        metadata[AIModelMetadataKeys.CapabilitySettingsUnsupported].ShouldBe("reasoningEffort");
        metadata[AIModelMetadataKeys.ProfileSettingsUnsupported].ShouldBe("temperature");
    }

    [Fact]
    public void ImageGetSettingsSupport_DallE3_DeclaresNothing()
    {
        // Style is a DALL·E 3 feature, so it is the one family where the field should render.
        CreateImageCapability().GetSettingsSupport("dall-e-3").ToMetadata().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("gpt-image-1")]
    [InlineData("dall-e-2")]
    [InlineData("some-future-image-model")]
    public void ImageGetSettingsSupport_ModelWithoutStyle_DeclaresStyleUnsupported(string modelId)
    {
        var metadata = CreateImageCapability().GetSettingsSupport(modelId).ToMetadata();

        metadata[AIModelMetadataKeys.CapabilitySettingsUnsupported].ShouldBe("style");
        // Quality applies to every family, just with different values, so it is never declared unsupported —
        // the per-request gate handles the vocabulary difference.
        metadata[AIModelMetadataKeys.CapabilitySettingsUnsupported].ShouldNotContain("quality");
    }

    private static OpenAIChatCapability CreateCapability()
        => new(
            new OpenAIProvider(new FakeProviderInfrastructure(), new MemoryCache(new MemoryCacheOptions())),
            logger: null);

    private static OpenAIImageGeneratorCapability CreateImageCapability()
        => new(
            new OpenAIProvider(new FakeProviderInfrastructure(), new MemoryCache(new MemoryCacheOptions())),
            logger: null);
}
