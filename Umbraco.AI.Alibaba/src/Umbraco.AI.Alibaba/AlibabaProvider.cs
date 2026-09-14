using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Alibaba;

/// <summary>
/// AI provider for Alibaba Cloud's Qwen models via Model Studio (DashScope). Model Studio exposes
/// an OpenAI-compatible API, so we reuse the OpenAI .NET SDK pointed at its endpoint.
/// </summary>
[AIProvider("alibaba", "Alibaba Cloud Model Studio")]
public class AlibabaProvider : AIProviderBase<AlibabaProviderSettings>
{
    private const string CacheKeyPrefix = "Alibaba_Models_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlibabaProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public AlibabaProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;
        WithCapability<AlibabaChatCapability>();
        WithCapability<AlibabaEmbeddingCapability>();
    }

    /// <summary>
    /// Gets all available models from Model Studio with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available model IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        AlibabaProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Alibaba Cloud API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        var client = CreateAlibabaClient(settings).GetOpenAIModelClient();
        var result = await client.GetModelsAsync(cancellationToken);

        var modelIds = result.Value
            .Select(m => m.Id)
            .OrderBy(id => id)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates an OpenAI-SDK client configured for Model Studio's OpenAI-compatible endpoint.
    /// </summary>
    internal static OpenAIClient CreateAlibabaClient(AlibabaProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Alibaba Cloud API key is required.");
        }

        var credential = new ApiKeyCredential(settings.ApiKey);
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? "https://dashscope-intl.aliyuncs.com/compatible-mode/v1"
            : settings.Endpoint;

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        };
        options.AddPolicy(new AlibabaDisableThinkingPolicy(), PipelinePosition.PerCall);

        return new OpenAIClient(credential, options);
    }

    private static string GetCacheKey(AlibabaProviderSettings settings)
    {
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{endpoint}";
    }
}
