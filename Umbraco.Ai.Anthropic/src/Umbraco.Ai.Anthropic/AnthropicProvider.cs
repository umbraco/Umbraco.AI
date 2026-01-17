using Anthropic;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Anthropic;

/// <summary>
/// AI provider for Anthropic services.
/// </summary>
[AiProvider("anthropic", "Anthropic")]
public class AnthropicProvider : AiProviderBase<AnthropicProviderSettings>
{
    private const string CacheKeyPrefix = "Anthropic_Models_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public AnthropicProvider(IAiProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;
        
        WithCapability<AnthropicChatCapability>();
    }

    /// <summary>
    /// Gets all available models from the Anthropic API with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available model IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        AnthropicProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Anthropic API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        var client = CreateAnthropicClient(settings);
        var result = await client.Models.List(cancellationToken: cancellationToken).ConfigureAwait(false);

        var modelIds = result.Items
            .Select(m => m.ID)
            .OrderBy(id => id)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates an Anthropic client configured with the provided settings.
    /// </summary>
    internal static AnthropicClient CreateAnthropicClient(AnthropicProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Anthropic API key is required.");
        }

        return string.IsNullOrWhiteSpace(settings.Endpoint)
            ? new AnthropicClient { ApiKey = settings.ApiKey }
            : new AnthropicClient
            {
                ApiKey = settings.ApiKey,
                BaseUrl = settings.Endpoint
            };
    }

    private static string GetCacheKey(AnthropicProviderSettings settings)
    {
        // Cache per API key + endpoint combination
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{endpoint}";
    }
}