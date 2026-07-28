using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// Anthropic's models endpoint reports a <c>capabilities</c> object per model, so which settings a model
/// accepts is available as data rather than inferred from its ID. These tests cover reading it, paging
/// through the response, and falling back to the ID predicate when the API says nothing.
/// </summary>
public class AnthropicModelCapabilityTests
{
    private const string TwoModelsPage = """
        {
          "data": [
            { "type": "model", "id": "claude-opus-5", "display_name": "Claude Opus 5", "created_at": "2026-06-09T00:00:00Z",
              "max_input_tokens": 1000000, "max_tokens": 128000,
              "capabilities": { "effort": { "supported": true, "low": { "supported": true }, "medium": { "supported": true },
                                            "high": { "supported": true }, "xhigh": { "supported": true }, "max": { "supported": true } },
                                "thinking": { "supported": true, "types": { "adaptive": { "supported": true }, "enabled": { "supported": false } } } } },
            { "type": "model", "id": "claude-haiku-4-5-20251001", "display_name": "Claude Haiku 4.5", "created_at": "2025-10-01T00:00:00Z",
              "max_input_tokens": 200000, "max_tokens": 64000,
              "capabilities": { "effort": { "supported": false },
                                "thinking": { "supported": true, "types": { "adaptive": { "supported": false }, "enabled": { "supported": true } } } } }
          ],
          "first_id": "claude-opus-5",
          "last_id": "claude-haiku-4-5-20251001",
          "has_more": false
        }
        """;

    [Fact]
    public async Task GetAvailableModelsAsync_ReadsReportedEffortSupportPerModel()
    {
        // Arrange
        var (provider, settings, _) = CreateProvider(TwoModelsPage);

        // Act
        var models = await provider.GetAvailableModelsAsync(settings);

        // Assert
        models.Single(m => m.Id == "claude-opus-5").SupportsEffort.ShouldBe(true);
        models.Single(m => m.Id == "claude-haiku-4-5-20251001").SupportsEffort.ShouldBe(false);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_ApiOmitsCapabilities_ReportsNotKnown()
    {
        // Arrange — a gateway or older API version that returns no capabilities object
        var (provider, settings, _) = CreateProvider("""
            { "data": [ { "type": "model", "id": "claude-opus-5", "display_name": "Claude Opus 5",
                          "created_at": "2026-06-09T00:00:00Z", "max_input_tokens": 1, "max_tokens": 1 } ],
              "has_more": false }
            """);

        // Act
        var models = await provider.GetAvailableModelsAsync(settings);

        // Assert — null rather than false, so the caller falls back to the ID predicate
        models.ShouldHaveSingleItem().SupportsEffort.ShouldBeNull();
    }

    [Fact]
    public async Task GetAvailableModelsAsync_PagedResponse_FollowsEveryPage()
    {
        // Arrange — the endpoint pages, and the previous code took only the first page
        var (provider, settings, handler) = CreateProvider(
            """
            { "data": [ { "type": "model", "id": "claude-opus-5", "display_name": "Claude Opus 5",
                          "created_at": "2026-06-09T00:00:00Z", "max_input_tokens": 1, "max_tokens": 1,
                          "capabilities": { "effort": { "supported": true } } } ],
              "first_id": "claude-opus-5", "last_id": "claude-opus-5", "has_more": true }
            """,
            """
            { "data": [ { "type": "model", "id": "claude-sonnet-4-6", "display_name": "Claude Sonnet 4.6",
                          "created_at": "2026-02-04T00:00:00Z", "max_input_tokens": 1, "max_tokens": 1,
                          "capabilities": { "effort": { "supported": true } } } ],
              "first_id": "claude-sonnet-4-6", "last_id": "claude-sonnet-4-6", "has_more": false }
            """);

        // Act
        var models = await provider.GetAvailableModelsAsync(settings);

        // Assert
        models.Select(m => m.Id).ShouldBe(["claude-opus-5", "claude-sonnet-4-6"]);
        handler.RequestUris.Count.ShouldBe(2);
        handler.RequestUris[0].ShouldContain("limit=1000");
        handler.RequestUris[1].ShouldContain("after_id=claude-opus-5");
    }

    [Fact]
    public async Task GetModelsAsync_DeclaresUnsupportedFromReportedCapabilities()
    {
        // Arrange
        var (provider, settings, _) = CreateProvider(TwoModelsPage);
        var capability = new AnthropicChatCapability(provider, logger: null);

        // Act
        var descriptors = await ((Core.Providers.IAICapability)capability).GetModelsAsync(settings);

        // Assert — Haiku 4.5 reports no effort support, so the setting is declared unsupported for it
        var haiku = descriptors.Single(d => d.Model.ModelId == "claude-haiku-4-5-20251001");
        haiku.Metadata[AIModelMetadataKeys.CapabilitySettingsUnsupported].ShouldBe("effort");

        // ... and Opus 5 reports support, so nothing is declared
        var opus = descriptors.Single(d => d.Model.ModelId == "claude-opus-5");
        opus.Metadata.ContainsKey(AIModelMetadataKeys.CapabilitySettingsUnsupported).ShouldBeFalse();
    }

    [Fact]
    public async Task GetModelsAsync_DeclaresTemperatureUnsupportedWhereTheModelRejectsIt()
    {
        // Arrange
        var (provider, settings, _) = CreateProvider(TwoModelsPage);
        var capability = new AnthropicChatCapability(provider, logger: null);

        // Act
        var descriptors = await ((Core.Providers.IAICapability)capability).GetModelsAsync(settings);

        // Assert — Opus 5 rejects the sampling parameters but reports effort support, and Haiku 4.5 is the
        // exact inverse, so the two declarations cannot be reading each other's source
        var opus = descriptors.Single(d => d.Model.ModelId == "claude-opus-5");
        opus.Metadata[AIModelMetadataKeys.ProfileSettingsUnsupported].ShouldBe("temperature");
        opus.Metadata.ContainsKey(AIModelMetadataKeys.CapabilitySettingsUnsupported).ShouldBeFalse();

        var haiku = descriptors.Single(d => d.Model.ModelId == "claude-haiku-4-5-20251001");
        haiku.Metadata.ContainsKey(AIModelMetadataKeys.ProfileSettingsUnsupported).ShouldBeFalse();
        haiku.Metadata[AIModelMetadataKeys.CapabilitySettingsUnsupported].ShouldBe("effort");
    }

    [Fact]
    public async Task TryGetModelCapability_AfterFetch_IsReadableWithoutIo()
    {
        // Arrange — this is what lets the per-request settings hook consult reported capabilities
        var (provider, settings, _) = CreateProvider(TwoModelsPage);

        // Act
        await provider.GetAvailableModelsAsync(settings);

        // Assert
        provider.TryGetModelCapability("claude-haiku-4-5-20251001")!.SupportsEffort.ShouldBe(false);
        provider.TryGetModelCapability("claude-opus-5")!.SupportsEffort.ShouldBe(true);
        provider.TryGetModelCapability("never-fetched-model").ShouldBeNull();
    }

    private static (AnthropicProvider Provider, AnthropicProviderSettings Settings, StubHttpMessageHandler Handler)
        CreateProvider(params string[] responses)
    {
        var handler = new StubHttpMessageHandler(responses);
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            handler);

        return (provider, new AnthropicProviderSettings { ApiKey = "test-key" }, handler);
    }

    /// <summary>
    /// A real provider with the model-list client pointed at a stub handler, so the SDK's own
    /// deserialization and paging are exercised against canned responses.
    /// </summary>
    [Core.Providers.AIProvider("anthropic", "Anthropic")]
    private sealed class StubbedAnthropicProvider(
        Core.Providers.IAIProviderInfrastructure infrastructure,
        IMemoryCache cache,
        HttpMessageHandler handler)
        : AnthropicProvider(infrastructure, cache)
    {
        internal override global::Anthropic.AnthropicClient CreateSdkClient(
            AnthropicProviderSettings settings)
            => new()
            {
                ApiKey = "test-key",
                MaxRetries = 0,
                HttpClient = new HttpClient(handler),
            };
    }
}
