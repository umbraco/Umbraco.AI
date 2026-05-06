using Microsoft.Extensions.Caching.Memory;
using Mistral.SDK;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Mistral;

/// <summary>
/// AI provider for Mistral services.
/// </summary>
[AIProvider("mistral", "Mistral")]
public class MistralProvider : AIProviderBase<MistralProviderSettings>
{
    private const string CacheKeyPrefix = "Mistral_Models_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="MistralProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public MistralProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;

        WithCapability<MistralChatCapability>();
        WithCapability<MistralEmbeddingCapability>();
    }

    /// <summary>
    /// Gets all available models from the Mistral API with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available model IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        MistralProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Mistral API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        var client = CreateMistralClient(settings);
        var result = await client.Models.GetModelsAsync();

        var modelIds = result.Data
            .Select(m => m.Id)
            .OrderBy(id => id)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates a Mistral client configured with the provided settings.
    /// </summary>
    internal static MistralClient CreateMistralClient(MistralProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Mistral API key is required.");
        }

        return new MistralClient(settings.ApiKey);
    }

    private static string GetCacheKey(MistralProviderSettings settings)
        => $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}";
}
