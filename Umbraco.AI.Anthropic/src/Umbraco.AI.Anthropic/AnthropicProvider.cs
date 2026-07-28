using Anthropic;
using Anthropic.Models.Models;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Anthropic.Errors;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Anthropic;

/// <summary>
/// AI provider for Anthropic services.
/// </summary>
[AIProvider("anthropic", "Anthropic")]
public class AnthropicProvider : AIProviderBase<AnthropicProviderSettings>
{
    private const string CacheKeyPrefix = "Anthropic_Models_";
    private const string ModelCapabilityCacheKeyPrefix = "Anthropic_ModelCapability_";

    /// <summary>
    /// The API's maximum page size for the models endpoint. Without it the endpoint pages at 20, which
    /// silently truncates the model list.
    /// </summary>
    private const int ModelPageSize = 1000;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public AnthropicProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;
        
        WithCapability<AnthropicChatCapability>();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Handles Anthropic's mid-stream SSE error envelope (e.g. <c>overloaded_error</c>, issue #174)
    /// and the SDK's HTTP-status exceptions, falling back to the shared transport mapping for
    /// anything else.
    /// </remarks>
    public override AIProviderErrorInfo ClassifyError(Exception exception)
        => AnthropicErrorMapping.TryClassify(exception) ?? base.ClassifyError(exception);

    /// <summary>
    /// Gets all available model IDs from the Anthropic API with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available model IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        AnthropicProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var models = await GetAvailableModelsAsync(settings, cancellationToken).ConfigureAwait(false);

        return models.Select(m => m.Id).OrderBy(id => id).ToList();
    }

    /// <summary>
    /// Gets all available models from the Anthropic API with caching, including the per-model capability
    /// facts the API reports.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The models endpoint reports a <c>capabilities</c> object per model, so which settings a model
    /// accepts is available as data rather than inferred from its ID. Each model's facts are additionally
    /// cached under their own key so the per-request path can read them without any I/O — see
    /// <see cref="TryGetModelCapability"/>.
    /// </remarks>
    internal async Task<IReadOnlyList<AnthropicModelCapability>> GetAvailableModelsAsync(
        AnthropicProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Anthropic API key is required.");
        }

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<AnthropicModelCapability>>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var client = CreateModelListClient(settings);
        var models = new List<AnthropicModelCapability>();

        var page = await client.Models.List(new ModelListParams { Limit = ModelPageSize }, cancellationToken);
        while (true)
        {
            foreach (var model in page.Items)
            {
                models.Add(new AnthropicModelCapability(model.ID, ReadEffortSupport(model)));
            }

            if (!page.HasNext())
            {
                break;
            }

            page = await page.Next(cancellationToken);
        }

        _cache.Set(cacheKey, (IReadOnlyList<AnthropicModelCapability>)models, CacheDuration);

        // Capability facts belong to the model, not to the connection, so they are cached per model ID as
        // well. This is what lets the per-request path consult them synchronously.
        foreach (var model in models)
        {
            _cache.Set(ModelCapabilityCacheKeyPrefix + model.Id, model, CacheDuration);
        }

        return models;
    }

    /// <summary>
    /// Reads a model's cached capability facts, or <c>null</c> when the model has not been fetched.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <remarks>
    /// Warm by construction on the request path: a chat client cannot exist without the capability having
    /// fetched the model list first. Returns null for a model absent from that list, where the caller
    /// falls back to inferring support from the ID.
    /// </remarks>
    internal AnthropicModelCapability? TryGetModelCapability(string? modelId)
        => string.IsNullOrWhiteSpace(modelId)
            ? null
            : _cache.TryGetValue<AnthropicModelCapability>(ModelCapabilityCacheKeyPrefix + modelId, out var cached)
                ? cached
                : null;

    /// <summary>
    /// Reads whether the API reported effort support for a model, treating an absent capabilities object
    /// as "not reported" rather than as unsupported.
    /// </summary>
    private static bool? ReadEffortSupport(ModelInfo model)
    {
        try
        {
            return model.Capabilities?.Effort?.Supported;
        }
        catch (Exception)
        {
            // Older API versions and non-first-party gateways may omit the capabilities object entirely.
            return null;
        }
    }

    /// <summary>
    /// Creates the client used to list models. Overridable so a test can serve canned responses without
    /// reaching the API; production behaviour is <see cref="CreateAnthropicClient"/>.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    internal virtual AnthropicClient CreateModelListClient(AnthropicProviderSettings settings)
        => CreateAnthropicClient(settings);

    /// <summary>
    /// Creates an Anthropic client configured with the provided settings.
    /// </summary>
    internal static AnthropicClient CreateAnthropicClient(AnthropicProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Anthropic API key is required.");
        }

        return string.IsNullOrWhiteSpace(settings.Endpoint)
            ? new AnthropicClient { ApiKey = settings.ApiKey }
            : new AnthropicClient
            {
                ApiKey = settings.ApiKey,
                BaseUrl = settings.Endpoint
            };
    }

    private static string GetCacheKey(AnthropicProviderSettings settings)
    {
        // Cache per API key + endpoint combination
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{endpoint}";
    }
}