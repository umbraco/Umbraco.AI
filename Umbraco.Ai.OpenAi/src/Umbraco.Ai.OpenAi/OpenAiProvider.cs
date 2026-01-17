using System.ClientModel;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.OpenAi;

/// <summary>
/// AI provider for OpenAI services.
/// </summary>
[AiProvider("openai", "OpenAI")]
public class OpenAiProvider : AiProviderBase<OpenAiProviderSettings>
{
    private const string CacheKeyPrefix = "OpenAi_Models_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public OpenAiProvider(IAiProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;
        WithCapability<OpenAiChatCapability>();
        WithCapability<OpenAiEmbeddingCapability>();
    }

    /// <summary>
    /// Gets all available models from the OpenAI API with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available model IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        OpenAiProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        var client = CreateOpenAiClient(settings).GetOpenAIModelClient();
        var result = await client.GetModelsAsync(cancellationToken).ConfigureAwait(false);

        var modelIds = result.Value
            .Select(m => m.Id)
            .OrderBy(id => id)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates an OpenAI client configured with the provided settings.
    /// </summary>
    internal static OpenAI.OpenAIClient CreateOpenAiClient(OpenAiProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is required.");
        }

        var credential = new ApiKeyCredential(settings.ApiKey);

        return string.IsNullOrWhiteSpace(settings.Endpoint)
            ? new OpenAI.OpenAIClient(credential)
            : new OpenAI.OpenAIClient(credential, new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri(settings.Endpoint)
            });
    }

    private static string GetCacheKey(OpenAiProviderSettings settings)
    {
        // Cache per API key + endpoint combination
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{endpoint}";
    }
}