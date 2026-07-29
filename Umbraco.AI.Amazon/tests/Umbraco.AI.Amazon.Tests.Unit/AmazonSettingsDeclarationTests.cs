using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Amazon.Tests.Unit.Fakes;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Amazon.Tests.Unit;

/// <summary>
/// What the capability declares per model, which is what the profile editor renders from. Bedrock IDs carry
/// a region prefix and a version suffix, so the rows here pin that the declaration is made against the
/// decorated ID the model list actually contains rather than a bare family name.
/// </summary>
public class AmazonSettingsDeclarationTests
{
    [Theory]
    [InlineData("anthropic.claude-opus-4-8-v1:0")]
    [InlineData("us.anthropic.claude-opus-4-7-v1:0")]
    [InlineData("anthropic.claude-opus-5-v1:0")]
    public void GetSettingsSupport_ModelRejectingSamplingParameters_DeclaresTheSamplingGroup(string modelId)
    {
        var metadata = CreateCapability().GetSettingsSupport(modelId).ToMetadata();

        metadata[AIModelMetadataKeys.ProfileSettingsUnsupported].ShouldBe("temperature,topP,topK,frequencyPenalty,presencePenalty");
    }

    [Theory]
    [InlineData("anthropic.claude-3-5-sonnet-20241022-v1:0")]
    [InlineData("eu.anthropic.claude-sonnet-4-6-v1")]
    [InlineData("amazon.nova-lite-v1:0")]
    public void GetSettingsSupport_ModelAcceptingSamplingParameters_DeclaresNothing(string modelId)
    {
        var metadata = CreateCapability().GetSettingsSupport(modelId).ToMetadata();

        metadata.ShouldBeEmpty();
    }

    private static AmazonChatCapability CreateCapability()
        => new(
            new AmazonProvider(new FakeProviderInfrastructure(), new MemoryCache(new MemoryCacheOptions())),
            logger: null);
}
