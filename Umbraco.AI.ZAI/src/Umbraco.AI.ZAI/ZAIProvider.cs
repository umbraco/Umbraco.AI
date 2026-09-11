using System.ClientModel;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.ZAI;

/// <summary>
/// AI provider for Z.AI's GLM models. Z.AI exposes an OpenAI-compatible API,
/// so we reuse the OpenAI .NET SDK pointed at Z.AI's endpoint.
/// </summary>
[AIProvider("zai", "Z.AI")]
public class ZAIProvider : AIProviderBase<ZAIProviderSettings>
{
    private const string CacheKeyPrefix = "ZAI_Models_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZAIProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public ZAIProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;
        WithCapability<ZAIChatCapability>();
    }

    /// <summary>
    /// Gets all available models from the Z.AI API with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available model IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        ZAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Z.AI API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        var client = CreateZAIClient(settings).GetOpenAIModelClient();
        var result = await client.GetModelsAsync(cancellationToken);

        var modelIds = result.Value
            .Select(m => m.Id)
            .OrderBy(id => id)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates an OpenAI-SDK client configured for Z.AI's OpenAI-compatible endpoint.
    /// </summary>
    internal static OpenAIClient CreateZAIClient(ZAIProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Z.AI API key is required.");
        }

        var credential = new ApiKeyCredential(settings.ApiKey);
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? "https://api.z.ai/api/paas/v4"
            : settings.Endpoint;

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        };

        return new OpenAIClient(credential, options);
    }

    private static string GetCacheKey(ZAIProviderSettings settings)
    {
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{endpoint}";
    }
}
