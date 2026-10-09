using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.OpenRouter.Tests.Unit.Fakes;

/// <summary>
/// Lists models the way core does: a real <see cref="OpenRouterProvider"/> with a real memory cache, its
/// chat capability resolved from the provider, and <see cref="IAICapability.GetModelsAsync"/> called through
/// the public interface. Only the HTTP layer is faked.
/// </summary>
internal static class OpenRouterModelListing
{
    public static async Task<AIModelDescriptor> ListSingleModelAsync(string modelsJson, string modelId)
    {
        var provider = new OpenRouterProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            new StubHttpClientFactory(modelsJson),
            NullLogger<OpenRouterProvider>.Instance);

        IAICapability capability = provider.GetCapability<IAIChatCapability>();
        var models = await capability.GetModelsAsync(new OpenRouterProviderSettings { ApiKey = "test-key" });

        return models.Single(m => m.Model.ModelId == modelId);
    }
}
