using System.ClientModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using OpenAI;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.OpenRouter;

/// <summary>
/// AI provider for OpenRouter.
/// </summary>
/// <remarks>
/// OpenRouter exposes an OpenAI-compatible Chat Completions API in front of hundreds of models from
/// dozens of vendors (OpenAI, Anthropic, Google, Meta, Mistral, DeepSeek, and more) behind a single
/// endpoint and API key. This provider wraps <see cref="OpenAIClient"/> with the OpenRouter endpoint,
/// the same approach used by <c>Umbraco.AI.FireworksAI</c> and <c>Umbraco.AI.TogetherAI</c>.
/// </remarks>
[AIProvider("openrouter", "OpenRouter")]
public class OpenRouterProvider : AIProviderBase<OpenRouterProviderSettings>
{
    private const string CacheKeyPrefix = "OpenRouter_Models_";
    private const string DefaultEndpoint = "https://openrouter.ai/api/v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    /// <summary>
    /// Key prefix for the per-model listing entries read by <see cref="TryGetModelInfo"/>, so a chat
    /// capability's <c>GetSettingsSupport</c> can read a model's declared parameters synchronously at
    /// request-enforcement time without an extra round trip.
    /// </summary>
    internal const string ModelInfoCacheKeyPrefix = "OpenRouter_ModelInfo_";

    private readonly IMemoryCache _cache;
    private CancellationTokenSource _cacheEvictionSource = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenRouterProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRouterProvider"/> class.
    /// </summary>
    public OpenRouterProvider(
        IAIProviderInfrastructure infrastructure,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenRouterProvider> logger)
        : base(infrastructure)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        WithCapability<OpenRouterChatCapability>();
    }

    /// <summary>
    /// Lists all models in the OpenRouter catalog, with per-model capability metadata. Cached for one
    /// hour per (api-key hash, endpoint) combination.
    /// </summary>
    internal async Task<IReadOnlyList<OpenRouterModelInfo>> GetAvailableModelsAsync(
        OpenRouterProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        ValidateSettings(settings);

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<OpenRouterModelInfo>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        // Cache miss — evict all previous per-model entries so TryGetModelInfo never serves a stale
        // entry for a model that has dropped out of the catalog.
        _cacheEvictionSource.Cancel();
        _cacheEvictionSource.Dispose();
        _cacheEvictionSource = new CancellationTokenSource();

        var models = await FetchModelsFromApiAsync(settings, cancellationToken);

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .AddExpirationToken(new CancellationChangeToken(_cacheEvictionSource.Token));
        _cache.Set(cacheKey, models, cacheOptions);

        foreach (var model in models)
        {
            _cache.Set(ModelInfoCacheKeyPrefix + model.Id, model, cacheOptions);
        }

        return models;
    }

    /// <summary>
    /// Reads a model's cached listing entry, or <c>null</c> when the model has not been fetched.
    /// </summary>
    /// <remarks>
    /// Warm by construction on the request path: a client cannot exist without the chat capability
    /// having fetched the model list first (see <see cref="OpenRouterChatCapability.CreateClientAsync"/>).
    /// Returns <c>null</c> for a model absent from that list, where the caller falls back to treating
    /// the model as fully supported — see <see cref="OpenRouterChatCapability.GetSettingsSupport"/>.
    /// </remarks>
    internal OpenRouterModelInfo? TryGetModelInfo(string? modelId)
        => string.IsNullOrWhiteSpace(modelId)
            ? null
            : _cache.TryGetValue<OpenRouterModelInfo>(ModelInfoCacheKeyPrefix + modelId, out var cached)
                ? cached
                : null;

    /// <summary>
    /// Creates an <see cref="OpenAIClient"/> configured for the OpenRouter endpoint.
    /// </summary>
    internal static OpenAIClient CreateOpenAIClient(OpenRouterProviderSettings settings)
    {
        ValidateSettings(settings);

        var credential = new ApiKeyCredential(settings.ApiKey!);
        var endpoint = new Uri(
            string.IsNullOrWhiteSpace(settings.Endpoint)
                ? DefaultEndpoint
                : settings.Endpoint);

        return new OpenAIClient(credential, new OpenAIClientOptions { Endpoint = endpoint });
    }

    private async Task<IReadOnlyList<OpenRouterModelInfo>> FetchModelsFromApiAsync(
        OpenRouterProviderSettings settings,
        CancellationToken cancellationToken)
    {
        var baseEndpoint = string.IsNullOrWhiteSpace(settings.Endpoint) ? DefaultEndpoint : settings.Endpoint!;
        var modelsUrl = $"{baseEndpoint.TrimEnd('/')}/models";

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, modelsUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "OpenRouter models API ({Url}) returned {StatusCode}: {Body}",
                    modelsUrl,
                    (int)response.StatusCode,
                    body);
                return [];
            }

            var modelsResponse = await response.Content
                .ReadFromJsonAsync<OpenRouterModelsResponse>(cancellationToken);

            return (modelsResponse?.Data ?? [])
                .OrderBy(m => m.Id, StringComparer.Ordinal)
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            // If the API call fails, return an empty list - GetSettingsSupport treats an unlisted
            // model as fully supported, and the user can still type a model id manually.
            _logger.LogWarning(ex, "Failed to fetch OpenRouter models.");
            return [];
        }
    }

    private static void ValidateSettings(OpenRouterProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("OpenRouter API key is required.");
        }
    }

    private static string GetCacheKey(OpenRouterProviderSettings settings)
    {
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{endpoint}";
    }
}
