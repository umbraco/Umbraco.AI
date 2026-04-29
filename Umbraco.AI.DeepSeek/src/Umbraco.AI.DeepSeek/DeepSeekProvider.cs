using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.DeepSeek;

/// <summary>
/// AI provider for DeepSeek services. DeepSeek exposes an OpenAI-compatible API,
/// so we reuse the OpenAI .NET SDK pointed at a DeepSeek endpoint.
/// </summary>
[AIProvider("deepseek", "DeepSeek")]
public class DeepSeekProvider : AIProviderBase<DeepSeekProviderSettings>
{
    private const string CacheKeyPrefix = "DeepSeek_Models_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeepSeekProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public DeepSeekProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;
        WithCapability<DeepSeekChatCapability>();
    }

    /// <summary>
    /// Gets all available models from the DeepSeek API with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available model IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        DeepSeekProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("DeepSeek API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        var client = CreateDeepSeekClient(settings).GetOpenAIModelClient();
        var result = await client.GetModelsAsync(cancellationToken);

        var modelIds = result.Value
            .Select(m => m.Id)
            .OrderBy(id => id)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates an OpenAI-SDK client configured for DeepSeek's OpenAI-compatible endpoint.
    /// </summary>
    internal static OpenAIClient CreateDeepSeekClient(DeepSeekProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("DeepSeek API key is required.");
        }

        var credential = new ApiKeyCredential(settings.ApiKey);
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? "https://api.deepseek.com"
            : settings.Endpoint;

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        };
        options.AddPolicy(new DeepSeekDisableThinkingPolicy(), PipelinePosition.PerCall);

        return new OpenAIClient(credential, options);
    }

    private static string GetCacheKey(DeepSeekProviderSettings settings)
    {
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{endpoint}";
    }
}
