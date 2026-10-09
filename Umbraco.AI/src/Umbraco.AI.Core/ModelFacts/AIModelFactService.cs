using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.Cms.Core.Cache;

namespace Umbraco.AI.Core.ModelFacts;

/// <summary>
/// Default <see cref="IAIModelFactService"/>: caches per model, calls providers concurrently under a time limit,
/// and isolates provider failures.
/// </summary>
internal sealed class AIModelFactService : IAIModelFactService
{
    private const string CacheKeyPrefix = "Umbraco.AI.ModelFacts";

    private readonly AIModelFactProviderCollection _providers;
    private readonly IAppPolicyCache _cache;
    private readonly IOptions<AIModelFactOptions> _options;
    private readonly ILogger<AIModelFactService> _logger;

    public AIModelFactService(
        AIModelFactProviderCollection providers,
        IAppPolicyCache cache,
        IOptions<AIModelFactOptions> options,
        ILogger<AIModelFactService> logger)
    {
        _providers = providers;
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
        AIModelFactContext context,
        IReadOnlyList<AIModelDescriptor> models,
        CancellationToken cancellationToken = default)
    {
        // Distinct requested models, first occurrence wins.
        var requested = models
            .GroupBy(m => m.Model.ModelId, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        var perProvider = await Task.WhenAll(
            _providers.Select(provider => GetProviderFactsAsync(provider, context, requested, cancellationToken)));

        var result = new Dictionary<string, IReadOnlyList<AIModelFact>>(StringComparer.Ordinal);
        foreach (var model in requested)
        {
            var facts = perProvider
                .SelectMany(byModel => byModel.TryGetValue(model.Model.ModelId, out var f) ? f : [])
                .OrderBy(f => f.Tone == AIModelFactTone.Warning ? 0 : 1) // OrderBy is stable
                .ToList();

            if (facts.Count > 0)
            {
                result[model.Model.ModelId] = facts;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets one provider's facts for the requested models, from cache where possible. Never throws except
    /// when <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetProviderFactsAsync(
        IAIModelFactProvider provider,
        AIModelFactContext context,
        IReadOnlyList<AIModelDescriptor> requested,
        CancellationToken cancellationToken)
    {
        var providerType = provider.GetType();
        var cacheDuration = provider.CacheDuration;
        var useCache = cacheDuration > TimeSpan.Zero;

        var facts = new Dictionary<string, IReadOnlyList<AIModelFact>>(StringComparer.Ordinal);
        var uncached = new List<AIModelDescriptor>();

        foreach (var model in requested)
        {
            if (useCache
                && _cache.Get(BuildCacheKey(providerType, context, model.Model.ModelId)) is IReadOnlyList<AIModelFact> hit)
            {
                if (hit.Count > 0)
                {
                    facts[model.Model.ModelId] = hit;
                }
            }
            else
            {
                uncached.Add(model);
            }
        }

        if (uncached.Count == 0)
        {
            return facts;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.Value.ProviderTimeout);

        try
        {
            // WaitAsync bounds the call even when a provider ignores the token.
            var fetched = await provider
                .GetModelFactsAsync(context, uncached, timeout.Token)
                .WaitAsync(timeout.Token);

            // Materialise, sanitise and cache inside the guard: a misbehaving provider result
            // (null facts, throwing enumeration) must only cost that provider, not the request.
            var sanitised = new Dictionary<string, IReadOnlyList<AIModelFact>>(StringComparer.Ordinal);
            foreach (var model in uncached)
            {
                IReadOnlyList<AIModelFact> modelFacts =
                    fetched is not null && fetched.TryGetValue(model.Model.ModelId, out var raw) && raw is not null
                        ? raw.Where(f => f is not null).Select(SanitizeFact).ToList()
                        : [];

                sanitised[model.Model.ModelId] = modelFacts;
            }

            if (useCache)
            {
                foreach (var (modelId, modelFacts) in sanitised)
                {
                    // Empty results are cached too, so "no facts" does not re-ask the provider.
                    var value = modelFacts;
                    _cache.Insert(BuildCacheKey(providerType, context, modelId), () => value, cacheDuration);
                }
            }

            foreach (var (modelId, modelFacts) in sanitised)
            {
                if (modelFacts.Count > 0)
                {
                    facts[modelId] = modelFacts;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Model fact provider {Provider} timed out after {Timeout} and was skipped.",
                providerType.FullName,
                _options.Value.ProviderTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Model fact provider {Provider} failed and was skipped.", providerType.FullName);
        }

        return facts;
    }

    private static string BuildCacheKey(Type providerType, AIModelFactContext context, string modelId)
        => $"{CacheKeyPrefix}:{providerType.FullName}:{context.ConnectionId}:{context.Capability}:{modelId}";

    /// <summary>Drops any <see cref="AIModelFact.Url"/> that is not an absolute http/https URL; keeps the rest normalised.</summary>
    private static AIModelFact SanitizeFact(AIModelFact fact)
    {
        var safeUrl = TryGetHttpUrl(fact.Url);
        if (fact.Url is null || safeUrl == fact.Url)
        {
            return fact;
        }

        return new AIModelFact
        {
            Key = fact.Key,
            Label = fact.Label,
            ShortLabel = fact.ShortLabel,
            Value = fact.Value,
            SortValue = fact.SortValue,
            Detail = fact.Detail,
            Tone = fact.Tone,
            Url = safeUrl,
        };
    }

    private static string? TryGetHttpUrl(string? url)
        => url is not null
           && Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : null;
}
