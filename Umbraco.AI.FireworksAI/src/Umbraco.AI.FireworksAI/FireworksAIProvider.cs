using System.ClientModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OpenAI;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.FireworksAI;

/// <summary>
/// AI provider for Fireworks AI.
/// </summary>
/// <remarks>
/// Fireworks AI exposes an OpenAI-compatible API, so this provider wraps
/// <see cref="OpenAIClient"/> with the Fireworks endpoint. Model listing uses
/// Fireworks' native models endpoint, which returns capability metadata used
/// to classify models as chat or embedding without hardcoded model lists.
/// </remarks>
[AIProvider("fireworks-ai", "Fireworks AI")]
public class FireworksAIProvider : AIProviderBase<FireworksAIProviderSettings>
{
    private const string CacheKeyPrefix = "FireworksAI_Models_";
    private const int PageSize = 200;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FireworksAIProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FireworksAIProvider"/> class.
    /// </summary>
    public FireworksAIProvider(
        IAIProviderInfrastructure infrastructure,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<FireworksAIProvider> logger)
        : base(infrastructure)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        WithCapability<FireworksAIChatCapability>();
        WithCapability<FireworksAIEmbeddingCapability>();
    }

    /// <summary>
    /// Lists all models visible to the configured account, with capability metadata.
    /// Cached for one hour per (api-key hash, account id, endpoint) combination.
    /// </summary>
    internal async Task<IReadOnlyList<FireworksAIModelInfo>> GetAvailableModelsAsync(
        FireworksAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        ValidateSettings(settings);

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<FireworksAIModelInfo>>(cacheKey, out var cachedModels)
            && cachedModels is not null)
        {
            return cachedModels;
        }

        var models = await FetchModelsFromApiAsync(settings, cancellationToken);

        _cache.Set(cacheKey, models, CacheDuration);
        return models;
    }

    /// <summary>
    /// Creates an <see cref="OpenAIClient"/> configured for the Fireworks AI endpoint.
    /// </summary>
    internal static OpenAIClient CreateOpenAIClient(FireworksAIProviderSettings settings)
    {
        ValidateSettings(settings);

        var credential = new ApiKeyCredential(settings.ApiKey!);
        var endpoint = new Uri(
            string.IsNullOrWhiteSpace(settings.Endpoint)
                ? "https://api.fireworks.ai/inference/v1"
                : settings.Endpoint);

        return new OpenAIClient(credential, new OpenAIClientOptions { Endpoint = endpoint });
    }

    private async Task<IReadOnlyList<FireworksAIModelInfo>> FetchModelsFromApiAsync(
        FireworksAIProviderSettings settings,
        CancellationToken cancellationToken)
    {
        var accountId = string.IsNullOrWhiteSpace(settings.AccountId)
            ? "fireworks"
            : settings.AccountId;

        // Native endpoint lives at api.fireworks.ai/v1/... (no /inference prefix).
        // We derive the host from the user's configured endpoint to honour proxy setups.
        var host = DeriveNativeApiHost(settings.Endpoint);
        var client = _httpClientFactory.CreateClient();

        var results = new List<FireworksAIModelInfo>();
        string? pageToken = null;

        do
        {
            var url = $"{host}/v1/accounts/{accountId}/models?pageSize={PageSize}";
            if (!string.IsNullOrEmpty(pageToken))
            {
                url += $"&pageToken={Uri.EscapeDataString(pageToken)}";
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Fireworks models API ({Url}) returned {StatusCode}: {Body}",
                    url,
                    (int)response.StatusCode,
                    body);

                throw new HttpRequestException(
                    $"Fireworks models API returned {(int)response.StatusCode} ({response.StatusCode}). {body}",
                    inner: null,
                    statusCode: response.StatusCode);
            }

            var page = await response.Content
                .ReadFromJsonAsync<FireworksAIModelsResponse>(cancellationToken);

            if (page?.Models is { Count: > 0 })
            {
                results.AddRange(page.Models);
            }

            pageToken = page?.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return results
            .Where(m => m.SupportsServerless)
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Derives the Fireworks native API host from the user-configured endpoint.
    /// The OpenAI-compatible endpoint is <c>https://api.fireworks.ai/inference/v1</c>;
    /// the native models endpoint lives at <c>https://api.fireworks.ai/v1/...</c> on the same host.
    /// </summary>
    private static string DeriveNativeApiHost(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return "https://api.fireworks.ai";
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return "https://api.fireworks.ai";
        }

        return $"{uri.Scheme}://{uri.Authority}";
    }

    private static void ValidateSettings(FireworksAIProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Fireworks AI API key is required.");
        }
    }

    private static string GetCacheKey(FireworksAIProviderSettings settings)
    {
        var account = string.IsNullOrWhiteSpace(settings.AccountId) ? "fireworks" : settings.AccountId;
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{account}:{endpoint}";
    }
}
