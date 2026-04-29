using System.ClientModel;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.HuggingFace;

/// <summary>
/// AI provider for Hugging Face Inference Providers (OpenAI-compatible router).
/// </summary>
[AIProvider("huggingface", "Hugging Face")]
public class HuggingFaceProvider : AIProviderBase<HuggingFaceProviderSettings>
{
    private const string CacheKeyPrefix = "HuggingFace_Models_";
    private const string WhoAmIUrl = "https://huggingface.co/api/whoami-v2";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    // The router's /v1/models endpoint is public — it returns 200 even with no token —
    // so we use the Hub's whoami-v2 endpoint (which always requires auth) to validate
    // credentials and surface bad keys via Test Connection.
    private static readonly HttpClient AuthProbeClient = new();

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="HuggingFaceProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public HuggingFaceProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;
        WithCapability<HuggingFaceChatCapability>();
    }

    /// <summary>
    /// Gets all available models from the Hugging Face router with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available model IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        HuggingFaceProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Hugging Face API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        await VerifyApiKeyAsync(settings.ApiKey!, cancellationToken);

        var client = CreateOpenAIClient(settings).GetOpenAIModelClient();
        var result = await client.GetModelsAsync(cancellationToken);

        var modelIds = result.Value
            .Select(m => m.Id)
            .OrderBy(id => id)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates an OpenAI client pointed at the configured Hugging Face router endpoint.
    /// </summary>
    internal static OpenAIClient CreateOpenAIClient(HuggingFaceProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Hugging Face API key is required.");
        }

        var credential = new ApiKeyCredential(settings.ApiKey);
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? "https://router.huggingface.co/v1"
            : settings.Endpoint;

        return new OpenAIClient(credential, new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        });
    }

    private static string GetCacheKey(HuggingFaceProviderSettings settings)
    {
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{endpoint}";
    }

    private static async Task VerifyApiKeyAsync(string apiKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, WhoAmIUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await AuthProbeClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("Hugging Face access token was rejected by the Hub (whoami-v2 returned " + (int)response.StatusCode + ").");
        }

        response.EnsureSuccessStatusCode();
    }
}
