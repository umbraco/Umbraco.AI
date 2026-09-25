#pragma warning disable UMBRACOAI_DECISION // Touches the experimental IAIDecisionClient/AIBinaryDecisionQuestion contract

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.TypeSafe;

/// <summary>
/// AI provider for TypeSafe AI (Jev), an experimental typed-decision model.
/// </summary>
/// <remarks>
/// Exposes only the experimental Decision capability (<see cref="TypeSafeDecisionCapability"/>) — Jev
/// has no chat or embedding models, and answers a single typed yes/no, choice, or score question per
/// call. Calls Jev directly over HTTP (<see cref="IHttpClientFactory"/> + <c>System.Text.Json</c>),
/// like <c>Umbraco.AI.FireworksAI</c>, since no official .NET SDK exists for this vendor.
/// </remarks>
[AIProvider("typesafe", "TypeSafe AI")]
public class TypeSafeProvider : AIProviderBase<TypeSafeProviderSettings>
{
    private const string CacheKeyPrefix = "TypeSafe_ConnectionProbe_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="httpClientFactory">Used to create the <see cref="HttpClient"/> the decision client sends requests with.</param>
    /// <param name="cache">Caches a successful connection probe (see <see cref="EnsureConnectionValidAsync"/>).</param>
    public TypeSafeProvider(IAIProviderInfrastructure infrastructure, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;

        WithCapability<TypeSafeDecisionCapability>();
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> for calling the TypeSafe AI API.
    /// </summary>
    /// <remarks>
    /// The client carries no preconfigured base address or auth header — <see cref="TypeSafeDecisionClient"/>
    /// builds each request from <see cref="TypeSafeProviderSettings"/> directly, the same way
    /// <c>FireworksAIProvider</c> builds its native-models request per call.
    /// </remarks>
    internal HttpClient CreateHttpClient() => _httpClientFactory.CreateClient(nameof(TypeSafeProvider));

    /// <summary>
    /// Validates that <paramref name="settings"/> can actually reach TypeSafe AI (Jev has no models
    /// endpoint to probe instead — see <see cref="TypeSafeDecisionCapability"/>'s remarks) by sending one
    /// tiny authenticated question through <paramref name="client"/> and discarding the answer.
    /// </summary>
    /// <remarks>
    /// A successful probe is cached for an hour, keyed by endpoint + a SHA-256 hash of the API key (never
    /// the raw key), so a repeated "Test connection" click or capability lookup doesn't re-spend a Jev
    /// call every time. A failed probe is never cached — any failure (401, 422, network, etc.) always
    /// propagates as an exception so the next call gets a fresh answer.
    /// </remarks>
    internal async Task EnsureConnectionValidAsync(
        TypeSafeProviderSettings settings,
        IAIDecisionClient client,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(settings);
        if (_cache.TryGetValue<bool>(cacheKey, out var cached) && cached)
        {
            return;
        }

        var probe = new AIBinaryDecisionQuestion { Instructions = "Reply true." };
        await client.AskAsync(probe, cancellationToken: cancellationToken);

        _cache.Set(cacheKey, true, CacheDuration);
    }

    /// <summary>
    /// Derives a cache key from the endpoint and a SHA-256 hash of the API key — never the raw key
    /// itself, so it's safe even if the cache were ever inspected or logged.
    /// </summary>
    private static string GetCacheKey(TypeSafeProviderSettings settings)
    {
        var endpoint = settings.Endpoint ?? "default";
        var apiKeyHash = HashApiKey(settings.ApiKey);
        return $"{CacheKeyPrefix}{apiKeyHash}:{endpoint}";
    }

    private static string HashApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return "none";
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes);
    }
}
