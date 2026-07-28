using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Core.Models;
using Umbraco.AI.OpenAI.Tests.Unit.Fakes;

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

    private static OpenAIChatCapability CreateCapability()
        => new(
            new OpenAIProvider(new FakeProviderInfrastructure(), new MemoryCache(new MemoryCacheOptions())),
            logger: null);
}
