#pragma warning disable UMBRACOAI_EXTRACTION // Registers the experimental extraction capability

using System.ClientModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Cloudflare;

/// <summary>
/// AI provider for Cloudflare Workers AI, exposing a Chat capability and the experimental Extraction capability.
/// </summary>
/// <remarks>
/// Chat goes through Workers AI's OpenAI-compatible route with the OpenAI client. Model listing, the connection
/// check and Extraction call Cloudflare directly over HTTP (<see cref="IHttpClientFactory"/> +
/// <c>System.Text.Json</c>), like <c>Umbraco.AI.TypeSafe</c>.
/// </remarks>
[AIProvider("cloudflare", "Cloudflare")]
public class CloudflareProvider : AIProviderBase<CloudflareProviderSettings>
{
    private const string CacheKeyPrefix = "Cloudflare_ConnectionCheck_";
    private const string ChatModelsCacheKeyPrefix = "Cloudflare_ChatModels_";
    private const int ModelSearchPageSize = 100;
    private const int MaxModelSearchPages = 10;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudflareProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="httpClientFactory">Used to create the <see cref="HttpClient"/> the extraction client sends requests with.</param>
    /// <param name="cache">Caches a successful connection check.</param>
    public CloudflareProvider(IAIProviderInfrastructure infrastructure, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;

        WithCapability<CloudflareChatCapability>();
        WithCapability<CloudflareExtractionCapability>();
    }

    /// <summary>
    /// Creates an <see cref="OpenAIClient"/> pointed at the account's OpenAI-compatible Workers AI route,
    /// authenticated with the Cloudflare API token.
    /// </summary>
    internal static OpenAIClient CreateOpenAIClient(CloudflareProviderSettings settings)
    {
        CloudflareApi.ValidateSettings(settings);

        var options = new OpenAIClientOptions
        {
            Endpoint = CloudflareApi.BuildAccountUri(settings, "ai/v1"),
            RetryPolicy = AIClientRetryPolicy.Default,
        };

        return new OpenAIClient(new ApiKeyCredential(settings.ApiToken!), options);
    }

    /// <summary>
    /// Lists the account's Text Generation models from Cloudflare's model search, following its pagination.
    /// The result is cached for an hour, keyed by endpoint, account and a SHA-256 hash of the token (never the
    /// raw token). A failed lookup is never cached and throws with Cloudflare's own error message.
    /// </summary>
    internal async Task<IReadOnlyList<string>> GetChatModelIdsAsync(
        CloudflareProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        CloudflareApi.ValidateSettings(settings);

        var cacheKey = ChatModelsCacheKeyPrefix + GetCacheKey(settings);
        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var modelIds = new List<string>();
        for (var page = 1; page <= MaxModelSearchPages; page++)
        {
            var uri = CloudflareApi.BuildAccountUri(
                settings,
                $"ai/models/search?task={Uri.EscapeDataString("Text Generation")}&per_page={ModelSearchPageSize}&page={page}");
            using var request = CloudflareApi.CreateRequest(HttpMethod.Get, uri, settings);
            using var response = await CreateHttpClient().SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw await CloudflareApi.CreateExceptionAsync(response, settings, cancellationToken);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var pageCount = 0;
            if (document.RootElement.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array)
            {
                foreach (var model in result.EnumerateArray())
                {
                    if (model.ValueKind == JsonValueKind.Object
                        && model.TryGetProperty("name", out var name)
                        && name.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(name.GetString()))
                    {
                        modelIds.Add(name.GetString()!);
                        pageCount++;
                    }
                }
            }

            // A short or empty page is the last one; otherwise keep reading until Cloudflare says there is no more.
            if (pageCount < ModelSearchPageSize || !HasMorePages(document.RootElement, page))
            {
                break;
            }
        }

        var ordered = modelIds.Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToList();
        _cache.Set(cacheKey, (IReadOnlyList<string>)ordered, CacheDuration);
        return ordered;
    }

    private static bool HasMorePages(JsonElement root, int page)
    {
        if (!root.TryGetProperty("result_info", out var info) || info.ValueKind != JsonValueKind.Object)
        {
            return true;
        }

        return info.TryGetProperty("total_pages", out var totalPages)
               && totalPages.ValueKind == JsonValueKind.Number
               && totalPages.TryGetInt32(out var total)
            ? page < total
            : true;
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> for calling the Cloudflare API. It carries no base address or
    /// auth header; each request is built from <see cref="CloudflareProviderSettings"/>.
    /// </summary>
    internal HttpClient CreateHttpClient() => _httpClientFactory.CreateClient(nameof(CloudflareProvider));

    /// <summary>
    /// Validates that <paramref name="settings"/> can reach Cloudflare by searching the account's models for
    /// Clairic. The call is free (no tokens are spent) and proves the account ID and token.
    /// </summary>
    /// <remarks>
    /// A successful check is cached for an hour, keyed by endpoint, account and a SHA-256 hash of the token
    /// (never the raw token). A failed check is never cached: any non-success status throws an
    /// <see cref="HttpRequestException"/> carrying Cloudflare's own error message.
    /// </remarks>
    internal async Task EnsureConnectionValidAsync(CloudflareProviderSettings settings, CancellationToken cancellationToken = default)
    {
        CloudflareApi.ValidateSettings(settings);

        var cacheKey = GetCacheKey(settings);
        if (_cache.TryGetValue<bool>(cacheKey, out var cached) && cached)
        {
            return;
        }

        var uri = CloudflareApi.BuildAccountUri(settings, "ai/models/search?search=clairic");
        using var request = CloudflareApi.CreateRequest(HttpMethod.Get, uri, settings);
        using var response = await CreateHttpClient().SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CloudflareApi.CreateExceptionAsync(response, settings, cancellationToken);
        }

        _cache.Set(cacheKey, true, CacheDuration);
    }

    /// <summary>
    /// Derives a cache key from the endpoint, account and a SHA-256 hash of the token, never the raw token.
    /// </summary>
    private static string GetCacheKey(CloudflareProviderSettings settings)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(settings.ApiToken ?? string.Empty)));
        return $"{CacheKeyPrefix}{tokenHash}:{settings.AccountId}:{settings.Endpoint ?? "default"}";
    }
}
