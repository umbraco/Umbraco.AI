using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// The sampling parameters are declared unsupported for every model: API version 2026-10-21 removed them.
/// </summary>
/// <remarks>
/// There is deliberately no wire assertion that temperature is absent: the SDK's request type no longer has
/// the fields, so such a test would not exercise this package. The declaration is what makes core strip
/// them and the editor hide them.
/// </remarks>
public class AnthropicSamplingParameterWireTests
{
    [Theory]
    [InlineData("claude-opus-5")]
    [InlineData("claude-sonnet-4-6")]
    [InlineData("claude-3-5-sonnet-20241022")]
    public void AnyModel_DeclaresTheSamplingProfileSettingsUnsupported(string modelId)
    {
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            new CapturingHttpMessageHandler());
        var capability = new AnthropicChatCapability(provider, logger: null);

        capability.GetSettingsSupport(modelId).UnsupportedProfileSettings
            .ShouldBe(AIProfileSettingKeys.Sampling);
    }
}
