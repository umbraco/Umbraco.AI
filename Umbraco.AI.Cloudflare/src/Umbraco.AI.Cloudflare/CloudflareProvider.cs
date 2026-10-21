#pragma warning disable UMBRACOAI_EXTRACTION // Registers the experimental extraction capability

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Cloudflare;

/// <summary>
/// AI provider for Cloudflare Workers AI, currently exposing only the experimental Extraction capability.
/// </summary>
/// <remarks>
/// Calls Cloudflare directly over HTTP (<see cref="IHttpClientFactory"/> + <c>System.Text.Json</c>), like
/// <c>Umbraco.AI.TypeSafe</c>. Named after the company so further Cloudflare capabilities can be added to
/// this package later.
/// </remarks>
[AIProvider("cloudflare", "Cloudflare")]
public class CloudflareProvider : AIProviderBase<CloudflareProviderSettings>
{
    private const string CacheKeyPrefix = "Cloudflare_ConnectionCheck_";
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

        WithCapability<CloudflareExtractionCapability>();
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
