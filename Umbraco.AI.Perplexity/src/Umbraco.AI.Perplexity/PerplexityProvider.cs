using System.ClientModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Perplexity;

/// <summary>
/// AI provider for Perplexity services. Perplexity exposes an OpenAI-compatible
/// chat completions API, so we reuse the OpenAI .NET SDK with a custom endpoint.
/// </summary>
[AIProvider("perplexity", "Perplexity")]
public class PerplexityProvider : AIProviderBase<PerplexityProviderSettings>
{
    private const string CacheKeyPrefix = "Perplexity_Models_";
    private const string PerplexityOwner = "perplexity";
    private const string PerplexityPrefix = "perplexity/";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerplexityProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public PerplexityProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;

        WithCapability<PerplexityChatCapability>();
    }

    /// <summary>
    /// Gets all Perplexity-owned models from the Perplexity API with caching.
    /// Filters to models owned by Perplexity (their Sonar family) and strips the
    /// "perplexity/" namespace prefix used by the Agent API listing so the IDs
    /// are ready for use with chat completions.
    /// </summary>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        PerplexityProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Perplexity API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        // Use raw HTTP rather than the OpenAI SDK because Perplexity's response shapes
        // don't always round-trip cleanly through the SDK's deserializer.
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? "https://api.perplexity.ai"
            : settings.Endpoint!.TrimEnd('/');

        using var http = new HttpClient { BaseAddress = new Uri(endpoint + "/") };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // /v1/models on Perplexity is unauthenticated, so it can't validate the key.
        // Probe /chat/completions with max_tokens=1 first — that's where invalid keys
        // surface as 401, and it doubles as a smoke test of the user's chat permission.
        await ProbeChatAuthAsync(http, cancellationToken);

        using var response = await http.GetAsync("v1/models", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Perplexity /v1/models failed with status {(int)response.StatusCode}: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<ModelListResponse>(cancellationToken)
            ?? new ModelListResponse();

        var modelIds = (payload.Data ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m.Id)
                     && (string.Equals(m.OwnedBy, PerplexityOwner, StringComparison.OrdinalIgnoreCase)
                         || m.Id!.StartsWith(PerplexityPrefix, StringComparison.OrdinalIgnoreCase)))
            .Select(m => m.Id!.StartsWith(PerplexityPrefix, StringComparison.OrdinalIgnoreCase)
                ? m.Id[PerplexityPrefix.Length..]
                : m.Id!)
            .OrderBy(id => id)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates an OpenAI-compatible client configured for Perplexity's API.
    /// </summary>
    internal static OpenAIClient CreatePerplexityClient(PerplexityProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Perplexity API key is required.");
        }

        var credential = new ApiKeyCredential(settings.ApiKey);
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? "https://api.perplexity.ai"
            : settings.Endpoint;

        return new OpenAIClient(credential, new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        });
    }

    private static string GetCacheKey(PerplexityProviderSettings settings)
    {
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{endpoint}";
    }

    private static async Task ProbeChatAuthAsync(HttpClient http, CancellationToken cancellationToken)
    {
        var probe = new
        {
            model = "sonar",
            messages = new[] { new { role = "user", content = "ping" } },
            max_tokens = 1,
            stream = false,
        };

        using var response = await http.PostAsJsonAsync("chat/completions", probe, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Perplexity API key validation failed (status {(int)response.StatusCode}): {body}");
    }

    private sealed class ModelListResponse
    {
        [JsonPropertyName("data")]
        public List<ModelEntry>? Data { get; set; }
    }

    private sealed class ModelEntry
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("owned_by")]
        public string? OwnedBy { get; set; }
    }
}
