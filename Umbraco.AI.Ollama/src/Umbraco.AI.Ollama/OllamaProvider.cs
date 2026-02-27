using Microsoft.Extensions.Caching.Memory;
using OllamaSharp;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Ollama;

/// <summary>
/// AI provider for Ollama services.
/// </summary>
[AIProvider("ollama", "Ollama")]
public class OllamaProvider : AIProviderBase<OllamaProviderSettings>
{
    private const string CacheKeyPrefix = "Ollama_Models_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public OllamaProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;

        WithCapability<OllamaChatCapability>();
    }

    /// <summary>
    /// Gets all available models from the Ollama API with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing endpoint and credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available model IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        OllamaProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        var client = CreateOllamaClient(settings);
        var models = await client.ListLocalModelsAsync(cancellationToken);

        var modelIds = models
            .Select(m => m.Name)
            .OrderBy(name => name)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates an Ollama client configured with the provided settings.
    /// </summary>
    internal static OllamaApiClient CreateOllamaClient(OllamaProviderSettings settings)
    {
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? "http://localhost:11434"
            : settings.Endpoint;

        var uri = new Uri(endpoint);

        // Create HTTP client with custom headers if needed
        HttpClient? httpClient = null;
        if (!string.IsNullOrWhiteSpace(settings.CustomHeaders))
        {
            httpClient = new HttpClient();
            var headers = ParseCustomHeaders(settings.CustomHeaders);
            foreach (var (name, value) in headers)
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
            }
        }

        // Create Ollama client
        var client = httpClient != null
            ? new OllamaApiClient(httpClient, endpoint)
            : new OllamaApiClient(uri);

        // Note: OllamaSharp doesn't have built-in API key support
        // If API key authentication is needed, it should be passed via CustomHeaders
        // as "Authorization: Bearer YOUR_API_KEY"

        return client;
    }

    private static IEnumerable<(string Name, string Value)> ParseCustomHeaders(string customHeaders)
    {
        var lines = customHeaders.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(':', 2);
            if (parts.Length == 2)
            {
                yield return (parts[0].Trim(), parts[1].Trim());
            }
        }
    }

    private static string GetCacheKey(OllamaProviderSettings settings)
    {
        // Cache per endpoint + API key combination
        var endpoint = settings.Endpoint ?? "default";
        var apiKeyHash = settings.ApiKey?.GetHashCode() ?? 0;
        return $"{CacheKeyPrefix}{endpoint}:{apiKeyHash}";
    }
}
