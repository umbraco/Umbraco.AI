using System.ClientModel;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Moonshot;

/// <summary>
/// AI provider for Moonshot AI (Kimi) services. Moonshot exposes an OpenAI-compatible API,
/// so we reuse the OpenAI .NET SDK pointed at a Moonshot endpoint.
/// </summary>
[AIProvider("moonshot", "Moonshot AI")]
public class MoonshotProvider : AIProviderBase<MoonshotProviderSettings>
{
    private const string CacheKeyPrefix = "Moonshot_Models_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoonshotProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public MoonshotProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;
        WithCapability<MoonshotChatCapability>();
    }

    /// <summary>
    /// Gets all available models from the Moonshot API with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available model IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        MoonshotProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Moonshot API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        var client = CreateMoonshotClient(settings).GetOpenAIModelClient();
        var result = await client.GetModelsAsync(cancellationToken);

        var modelIds = result.Value
            .Select(m => m.Id)
            .OrderBy(id => id)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates an OpenAI-SDK client configured for Moonshot's OpenAI-compatible endpoint.
    /// </summary>
    internal static OpenAIClient CreateMoonshotClient(MoonshotProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Moonshot API key is required.");
        }

        var credential = new ApiKeyCredential(settings.ApiKey);
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? "https://api.moonshot.ai/v1"
            : settings.Endpoint;

        return new OpenAIClient(credential, new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        });
    }

    private static string GetCacheKey(MoonshotProviderSettings settings)
    {
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{endpoint}";
    }
}
