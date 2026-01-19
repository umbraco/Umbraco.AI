using Google.GenAI;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Gemini;

/// <summary>
/// AI provider for Google Gemini services.
/// </summary>
[AiProvider("gemini", "Google Gemini")]
public class GeminiProvider : AiProviderBase<GeminiProviderSettings>
{
    private const string CacheKeyPrefix = "Gemini_Models_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public GeminiProvider(IAiProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;
        WithCapability<GeminiChatCapability>();
    }

    /// <summary>
    /// Gets all available models from the Gemini API with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available model IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        GeminiProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        var client = CreateGeminiClient(settings);
        var modelsPager = await client.Models.ListAsync().ConfigureAwait(false);

        var allModels = new List<string>();
        await foreach (var model in modelsPager.WithCancellation(cancellationToken))
        {
            if (model.Name is not null && model.Name.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
            {
                allModels.Add(model.Name.Replace("models/", string.Empty));
            }
        }

        var modelIds = allModels.OrderBy(id => id).ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates a Gemini client configured with the provided settings.
    /// </summary>
    internal static Client CreateGeminiClient(GeminiProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is required.");
        }

        return new Client(apiKey: settings.ApiKey);
    }

    private static string GetCacheKey(GeminiProviderSettings settings)
    {
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}";
    }
}
