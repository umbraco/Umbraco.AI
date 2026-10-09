#if MODEL_FACTS_PENDING // Pending: T5 — remove this guard (and the matching #endif) in the commit that makes these specs pass.
// MF-4 — OpenRouter reports context window and price
using Umbraco.AI.Core.Models;
using Umbraco.AI.Extensions;
using Umbraco.AI.OpenRouter.Tests.Unit.Fakes;

namespace Umbraco.AI.OpenRouter.Tests.Unit;

/// <summary>
/// OpenRouter's <c>GET /models</c> reports each model's <c>context_length</c> and per-token USD
/// <c>pricing</c>. The chat capability's model listing writes them to the standard Metadata keys, so
/// core's readers see them without knowing about OpenRouter. Every scenario lists models through the
/// real provider and chat capability; only the HTTP response is canned.
/// </summary>
public class OpenRouterModelFactsTests
{
    // Shapes match a live OpenRouter listing entry: context_length is a number, pricing values are
    // per-token USD strings, and the entry carries fields this provider does not read.
    private const string ClaudeSonnet4Listing = """
        {
          "data": [
            {
              "id": "anthropic/claude-sonnet-4",
              "canonical_slug": "anthropic/claude-4-sonnet-20250522",
              "name": "Anthropic: Claude Sonnet 4",
              "created": 1747930371,
              "description": "Claude Sonnet 4 significantly enhances the capabilities of its predecessor.",
              "context_length": 200000,
              "architecture": {
                "modality": "text+image->text",
                "input_modalities": ["image", "text", "file"],
                "output_modalities": ["text"],
                "tokenizer": "Claude",
                "instruct_type": null
              },
              "pricing": {
                "prompt": "0.000003",
                "completion": "0.000015",
                "request": "0",
                "image": "0.0048",
                "web_search": "0",
                "internal_reasoning": "0",
                "input_cache_read": "0.0000003",
                "input_cache_write": "0.00000375"
              },
              "top_provider": { "context_length": 200000, "max_completion_tokens": 64000, "is_moderated": true },
              "per_request_limits": null,
              "supported_parameters": [
                "include_reasoning", "max_tokens", "reasoning", "response_format", "stop",
                "structured_outputs", "temperature", "tool_choice", "tools", "top_k", "top_p"
              ]
            }
          ]
        }
        """;

    private const string O3Listing = """
        {
          "data": [
            {
              "id": "openai/o3",
              "name": "OpenAI: o3",
              "created": 1744823457,
              "context_length": 200000,
              "architecture": { "modality": "text+image->text", "tokenizer": "GPT", "instruct_type": null },
              "pricing": { "prompt": "0.000002", "completion": "0.000008", "request": "0", "image": "0.00153" },
              "top_provider": { "context_length": 200000, "max_completion_tokens": 100000, "is_moderated": true },
              "per_request_limits": null,
              "supported_parameters": [
                "include_reasoning", "max_tokens", "reasoning", "response_format", "seed",
                "structured_outputs", "tool_choice", "tools"
              ]
            }
          ]
        }
        """;

    private const string NoContextLengthListing = """
        {
          "data": [
            {
              "id": "acme/mystery-model",
              "name": "Acme: Mystery Model",
              "created": 1750000000,
              "architecture": { "modality": "text->text", "tokenizer": "Other", "instruct_type": null },
              "pricing": { "prompt": "0.000001", "completion": "0.000002", "request": "0", "image": "0" },
              "top_provider": { "context_length": null, "max_completion_tokens": null, "is_moderated": false },
              "per_request_limits": null,
              "supported_parameters": ["max_tokens", "temperature", "top_p"]
            }
          ]
        }
        """;

    private const string FreeModelListing = """
        {
          "data": [
            {
              "id": "meta-llama/llama-3.3-70b-instruct:free",
              "name": "Meta: Llama 3.3 70B Instruct (free)",
              "created": 1733506137,
              "context_length": 131072,
              "architecture": { "modality": "text->text", "tokenizer": "Llama3", "instruct_type": "llama3" },
              "pricing": { "prompt": "0", "completion": "0", "request": "0", "image": "0" },
              "top_provider": { "context_length": 131072, "max_completion_tokens": 2048, "is_moderated": false },
              "per_request_limits": null,
              "supported_parameters": ["max_tokens", "temperature", "top_p", "top_k", "stop"]
            }
          ]
        }
        """;

    // openrouter/auto routes to another model per request, so the listing has no fixed price and uses
    // "-1" as a sentinel.
    private const string NegativeSentinelPricingListing = """
        {
          "data": [
            {
              "id": "openrouter/auto",
              "name": "Auto Router",
              "created": 1699401600,
              "context_length": 2000000,
              "architecture": { "modality": "text+image->text", "tokenizer": "Router", "instruct_type": null },
              "pricing": { "prompt": "-1", "completion": "-1" },
              "top_provider": { "context_length": null, "max_completion_tokens": null, "is_moderated": false },
              "per_request_limits": null,
              "supported_parameters": []
            }
          ]
        }
        """;

    private const string NonNumericPricingListing = """
        {
          "data": [
            {
              "id": "acme/unpriced-model",
              "name": "Acme: Unpriced Model",
              "created": 1750000000,
              "context_length": 32768,
              "architecture": { "modality": "text->text", "tokenizer": "Other", "instruct_type": null },
              "pricing": { "prompt": "n/a", "completion": "n/a" },
              "top_provider": { "context_length": 32768, "max_completion_tokens": null, "is_moderated": false },
              "per_request_limits": null,
              "supported_parameters": ["max_tokens", "temperature"]
            }
          ]
        }
        """;

    // ---------------------------------------------------------------------------------------------
    // Happy path
    // ---------------------------------------------------------------------------------------------

    public class GivenAListingEntryWithContextLengthAndPricing : IAsyncLifetime
    {
        private AIModelDescriptor _model = null!;

        public async Task InitializeAsync()
            => _model = await OpenRouterModelListing.ListSingleModelAsync(
                ClaudeSonnet4Listing, "anthropic/claude-sonnet-4");

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public void AC1_ReportsTheContextLengthAsTheContextWindow()
            => _model.GetContextWindow().ShouldBe(200000);

        [Fact]
        public void AC2_ReportsPerTokenPricingPerMillionTokensInUsd()
            => _model.GetPricing().ShouldBe(new AIModelPricing(3.00m, 15.00m, "USD"));
    }

    public class GivenAListingEntryWhoseSupportedParametersLackTemperature : IAsyncLifetime
    {
        private AIModelDescriptor _model = null!;

        public async Task InitializeAsync()
            => _model = await OpenRouterModelListing.ListSingleModelAsync(O3Listing, "openai/o3");

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public void AC3_StillDeclaresTemperatureUnsupported()
            => _model.IsProfileSettingSupported(AIProfileSettingKeys.Temperature).ShouldBeFalse();
    }

    // ---------------------------------------------------------------------------------------------
    // Sad path
    // ---------------------------------------------------------------------------------------------

    public class GivenAListingEntryWithNoContextLength : IAsyncLifetime
    {
        private AIModelDescriptor _model = null!;

        public async Task InitializeAsync()
            => _model = await OpenRouterModelListing.ListSingleModelAsync(
                NoContextLengthListing, "acme/mystery-model");

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public void AC4_ReportsNoContextWindow()
            => _model.GetContextWindow().ShouldBeNull();
    }

    public class GivenAListingEntryWithZeroPricing : IAsyncLifetime
    {
        private AIModelDescriptor _model = null!;

        public async Task InitializeAsync()
            => _model = await OpenRouterModelListing.ListSingleModelAsync(
                FreeModelListing, "meta-llama/llama-3.3-70b-instruct:free");

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public void AC5_ReportsNoPrice()
            => _model.GetPricing().ShouldBeNull();
    }

    public class GivenAListingEntryWithNegativeSentinelPricing : IAsyncLifetime
    {
        private AIModelDescriptor _model = null!;

        public async Task InitializeAsync()
            => _model = await OpenRouterModelListing.ListSingleModelAsync(
                NegativeSentinelPricingListing, "openrouter/auto");

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public void AC6_ReportsNoPrice()
            => _model.GetPricing().ShouldBeNull();
    }

    public class GivenAListingEntryWithNonNumericPricing : IAsyncLifetime
    {
        private AIModelDescriptor _model = null!;

        public async Task InitializeAsync()
            => _model = await OpenRouterModelListing.ListSingleModelAsync(
                NonNumericPricingListing, "acme/unpriced-model");

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public void AC6_ReportsNoPrice()
            => _model.GetPricing().ShouldBeNull();
    }
}
#endif
