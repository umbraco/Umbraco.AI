using System.ClientModel;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.TogetherAI;

/// <summary>
/// AI provider for Together AI services. Together AI exposes an OpenAI-compatible
/// API at <c>https://api.together.xyz/v1</c>, so the OpenAI SDK is reused with a
/// custom endpoint.
/// </summary>
[AIProvider("togetherai", "Together AI")]
public class TogetherAIProvider : AIProviderBase<TogetherAIProviderSettings>
{
    private const string CacheKeyPrefix = "TogetherAI_Models_";
    private const string DefaultEndpoint = "https://api.together.xyz/v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="TogetherAIProvider"/> class.
    /// </summary>
    public TogetherAIProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;

        WithCapability<TogetherAIChatCapability>();
        WithCapability<TogetherAIEmbeddingCapability>();
    }

    /// <summary>
    /// Gets all available models from the Together AI <c>/v1/models</c> endpoint with caching.
    /// Each model is returned with its declared <c>type</c> (e.g. <c>chat</c>, <c>embedding</c>,
    /// <c>image</c>, <c>moderation</c>, <c>rerank</c>, <c>audio</c>, <c>language</c>) so that
    /// capabilities can filter dynamically without hard-coded model lists.
    /// </summary>
    internal async Task<IReadOnlyList<TogetherAIModelInfo>> GetAvailableModelsAsync(
        TogetherAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Together AI API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<TogetherAIModelInfo>>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var models = await FetchModelsAsync(settings, cancellationToken);
        _cache.Set(cacheKey, models, CacheDuration);
        return models;
    }

    /// <summary>
    /// Creates an <see cref="OpenAIClient"/> configured to talk to the Together AI endpoint.
    /// </summary>
    internal static OpenAIClient CreateOpenAIClient(TogetherAIProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Together AI API key is required.");
        }

        var credential = new ApiKeyCredential(settings.ApiKey);
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint) ? DefaultEndpoint : settings.Endpoint;

        return new OpenAIClient(credential, new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        });
    }

    private static async Task<IReadOnlyList<TogetherAIModelInfo>> FetchModelsAsync(
        TogetherAIProviderSettings settings,
        CancellationToken cancellationToken)
    {
        // The OpenAI 2.x SDK's typed GetModelsAsync drops Together's extra `type` field,
        // so go to the raw HTTP endpoint to keep filtering dynamic.
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint) ? DefaultEndpoint : settings.Endpoint!;
        var modelsUri = new Uri(endpoint.TrimEnd('/') + "/models");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await http.GetAsync(modelsUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        // Together returns either a top-level array or an OpenAI-shaped { "data": [...] }.
        var root = doc.RootElement.ValueKind == JsonValueKind.Array
            ? doc.RootElement
            : doc.RootElement.TryGetProperty("data", out var data) ? data : default;

        if (root.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<TogetherAIModelInfo>();
        }

        var models = new List<TogetherAIModelInfo>(root.GetArrayLength());
        foreach (var element in root.EnumerateArray())
        {
            if (!element.TryGetProperty("id", out var idProp) || idProp.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var id = idProp.GetString();
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var type = element.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String
                ? typeProp.GetString()
                : null;

            var displayName = element.TryGetProperty("display_name", out var dn) && dn.ValueKind == JsonValueKind.String
                ? dn.GetString()
                : null;

            models.Add(new TogetherAIModelInfo(id, type, displayName));
        }

        return models
            .OrderBy(m => m.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static string GetCacheKey(TogetherAIProviderSettings settings)
    {
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{endpoint}";
    }
}

/// <summary>
/// A model exposed by the Together AI <c>/v1/models</c> endpoint.
/// </summary>
/// <param name="Id">Model identifier (e.g. <c>meta-llama/Llama-3.3-70B-Instruct-Turbo</c>).</param>
/// <param name="Type">Together's declared model type (e.g. <c>chat</c>, <c>embedding</c>). May be null for older API responses.</param>
/// <param name="DisplayName">Optional vendor-supplied display name; preferred over generated names when present.</param>
internal sealed record TogetherAIModelInfo(string Id, string? Type, string? DisplayName);
