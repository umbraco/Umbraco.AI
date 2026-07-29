using System.ClientModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OpenAI;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// AI provider for Microsoft AI Foundry (Azure AI).
/// </summary>
/// <remarks>
/// This provider supports all models available through Microsoft AI Foundry's unified endpoint,
/// including OpenAI models (GPT-4, GPT-4o), Mistral, Llama, Cohere, Phi, and more.
/// One endpoint and API key provides access to all deployed models.
/// Supports both API key and Entra ID authentication.
/// </remarks>
[AIProvider("microsoft-foundry", "Microsoft AI Foundry")]
public class MicrosoftFoundryProvider : AIProviderBase<MicrosoftFoundryProviderSettings>
{
    private const string CacheKeyPrefix = "MicrosoftFoundry_Models_";
    private const string ApiVersion = "2024-10-21";
    private const string CognitiveServicesScope = "https://cognitiveservices.azure.com/.default";
    private const string AiFoundryScope = "https://ai.azure.com/.default";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    /// <summary>
    /// Key prefix for the per-model listing entries read by <see cref="TryGetModelInfo"/>. Internal so a
    /// test can prime the cache the provider was constructed with, rather than standing up Entra ID auth
    /// and the deployments endpoint to reach the same state.
    /// </summary>
    internal const string ModelInfoCacheKeyPrefix = "MicrosoftFoundry_ModelInfo_";

    private readonly IMemoryCache _cache;
    private CancellationTokenSource _cacheEvictionSource = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MicrosoftFoundryProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftFoundryProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public MicrosoftFoundryProvider(
        IAIProviderInfrastructure infrastructure,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<MicrosoftFoundryProvider> logger)
        : base(infrastructure)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        WithCapability<MicrosoftFoundryChatCapability>();
        WithCapability<MicrosoftFoundryEmbeddingCapability>();
    }

    /// <summary>
    /// Gets all available models from Microsoft AI Foundry with caching.
    /// </summary>
    /// <param name="settings">The provider settings containing API credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available models with their capabilities.</returns>
    internal async Task<IReadOnlyList<MicrosoftFoundryModelInfo>> GetAvailableModelsAsync(
        MicrosoftFoundryProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        ValidateSettings(settings);

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<MicrosoftFoundryModelInfo>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        // Cache miss — evict all previous entries
        _cacheEvictionSource.Cancel();
        _cacheEvictionSource.Dispose();
        _cacheEvictionSource = new CancellationTokenSource();

        IReadOnlyList<MicrosoftFoundryModelInfo> models;

        // When Entra ID is configured with a project name, use the deployments API to list only deployed models.
        // Fall back to the models API if the deployments call fails.
        if (HasEntraIdCredentials(settings) && !string.IsNullOrWhiteSpace(settings.ProjectName))
        {
            models = await FetchDeploymentsFromApiAsync(settings, cancellationToken);

            if (models.Count == 0)
            {
                _logger.LogWarning("Deployments API returned no results; falling back to models API.");
                models = await FetchModelsFromApiAsync(settings, cancellationToken);
            }
        }
        else
        {
            models = await FetchModelsFromApiAsync(settings, cancellationToken);
        }

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .AddExpirationToken(new Microsoft.Extensions.Primitives.CancellationChangeToken(_cacheEvictionSource.Token));
        _cache.Set(cacheKey, models, cacheOptions);

        // Also cache each model on its own key, which is what lets the per-request path read what a
        // deployment fronts without any I/O — see TryGetModelInfo. The same eviction token is used, so
        // these never outlive the listing they came from.
        foreach (var model in models)
        {
            _cache.Set(ModelInfoCacheKeyPrefix + model.Id, model, cacheOptions);
        }

        return models;
    }

    /// <summary>
    /// Reads a model's cached listing entry, or <c>null</c> when the model has not been fetched.
    /// </summary>
    /// <param name="modelId">The model ID, which on the deployments API path is the deployment name.</param>
    /// <remarks>
    /// <para>
    /// Warm by construction on the request path: a client cannot exist without the capability having
    /// fetched the model list first. Returns <c>null</c> for a model absent from that list, where callers
    /// fall back to reasoning from the ID alone.
    /// </para>
    /// <para>
    /// Keyed by model ID rather than per connection, because the per-request hook that reads this is given
    /// only a model ID. Two connections on the same Foundry provider can therefore collide if they use the
    /// same deployment name for different models, and the most recent listing wins. The consequence is
    /// bounded: a wrong entry can only cause the same silently-dropped parameter, or the same 400, that
    /// would happen without any of this metadata — never a new failure.
    /// </para>
    /// </remarks>
    internal MicrosoftFoundryModelInfo? TryGetModelInfo(string? modelId)
        => string.IsNullOrWhiteSpace(modelId)
            ? null
            : _cache.TryGetValue<MicrosoftFoundryModelInfo>(ModelInfoCacheKeyPrefix + modelId, out var cached)
                ? cached
                : null;

    /// <summary>
    /// Creates an <see cref="AzureOpenAIClient"/> configured with the provided settings.
    /// Uses Entra ID authentication when configured, otherwise falls back to API key.
    /// </summary>
    /// <param name="settings">The provider settings.</param>
    /// <returns>A configured AzureOpenAIClient.</returns>
    internal static AzureOpenAIClient CreateAzureOpenAIClient(MicrosoftFoundryProviderSettings settings)
    {
        ValidateSettings(settings);

        var endpoint = new Uri(settings.Endpoint!);

        if (HasEntraIdCredentials(settings))
        {
            return new AzureOpenAIClient(endpoint, BuildTokenCredential(settings));
        }

        return new AzureOpenAIClient(endpoint, new ApiKeyCredential(settings.ApiKey!));
    }

    /// <summary>
    /// Creates an <see cref="OpenAIClient"/> configured for the Responses API.
    /// Uses the <c>{endpoint}/openai/v1/</c> base URL pattern required by AI Foundry.
    /// </summary>
    /// <param name="settings">The provider settings.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>A configured OpenAIClient.</returns>
    internal static OpenAIClient CreateOpenAIClient(MicrosoftFoundryProviderSettings settings, ILogger? logger = null)
    {
        ValidateSettings(settings);

        var endpoint = new Uri($"{settings.Endpoint!.TrimEnd('/')}/openai/v1/");

        if (HasEntraIdCredentials(settings))
        {
            try
            {
                var token = BuildTokenCredential(settings)
                    .GetToken(new Azure.Core.TokenRequestContext([CognitiveServicesScope]), default);
                return new OpenAIClient(new ApiKeyCredential(token.Token), new OpenAIClientOptions { Endpoint = endpoint });
            }
            catch (Azure.Identity.AuthenticationFailedException ex)
            {
                logger?.LogError(ex, "Failed to acquire Entra ID token for Responses API (endpoint: {Endpoint}).", settings.Endpoint);
                throw;
            }
        }

        return new OpenAIClient(new ApiKeyCredential(settings.ApiKey!), new OpenAIClientOptions { Endpoint = endpoint });
    }

    /// <summary>
    /// Returns <c>true</c> if the settings contain any Entra ID credentials.
    /// </summary>
    internal static bool HasEntraIdCredentials(MicrosoftFoundryProviderSettings settings)
        => !string.IsNullOrWhiteSpace(settings.TenantId)
           || !string.IsNullOrWhiteSpace(settings.ClientId)
           || !string.IsNullOrWhiteSpace(settings.ClientSecret);

    /// <summary>
    /// Returns <c>true</c> if the settings contain an API key.
    /// </summary>
    internal static bool HasApiKeyCredentials(MicrosoftFoundryProviderSettings settings)
        => !string.IsNullOrWhiteSpace(settings.ApiKey);

    /// <summary>
    /// Builds a <see cref="Azure.Core.TokenCredential"/> based on the provided settings.
    /// If all three Entra ID fields (TenantId, ClientId, ClientSecret) are set, returns a
    /// <see cref="ClientSecretCredential"/>. Otherwise returns a <see cref="DefaultAzureCredential"/>
    /// for managed identity or development scenarios.
    /// </summary>
    private static Azure.Core.TokenCredential BuildTokenCredential(MicrosoftFoundryProviderSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.TenantId)
            && !string.IsNullOrWhiteSpace(settings.ClientId)
            && !string.IsNullOrWhiteSpace(settings.ClientSecret))
        {
            return new ClientSecretCredential(settings.TenantId, settings.ClientId, settings.ClientSecret);
        }

        // Fall back to DefaultAzureCredential for managed identity / dev scenarios
        var options = new DefaultAzureCredentialOptions();

        if (!string.IsNullOrWhiteSpace(settings.TenantId))
        {
            options.TenantId = settings.TenantId;
        }

        return new DefaultAzureCredential(options);
    }

    private async Task<IReadOnlyList<MicrosoftFoundryModelInfo>> FetchDeploymentsFromApiAsync(
        MicrosoftFoundryProviderSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var tokenCredential = BuildTokenCredential(settings);
            var tokenRequestContext = new Azure.Core.TokenRequestContext([AiFoundryScope]);
            var accessToken = await tokenCredential.GetTokenAsync(tokenRequestContext, cancellationToken);

            var client = _httpClientFactory.CreateClient();
            var baseEndpoint = settings.Endpoint!.TrimEnd('/');
            var deploymentsUrl = $"{baseEndpoint}/api/projects/{settings.ProjectName}/deployments?api-version=v1";

            // Azure's authorization layer can return transient 401s when a token is first used
            // against a resource (e.g., after credential changes). A single retry resolves this.
            HttpResponseMessage response;
            const int maxRetries = 1;

            for (var attempt = 0; ; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, deploymentsUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
                response = await client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode || attempt >= maxRetries)
                {
                    break;
                }

                _logger.LogDebug("Deployments API returned {StatusCode}, retrying...", (int)response.StatusCode);
                response.Dispose();
                await Task.Delay(500, cancellationToken);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Deployments API ({Url}) returned {StatusCode}: {Body}",
                    deploymentsUrl,
                    (int)response.StatusCode,
                    body);
                response.Dispose();
                return [];
            }

            var deploymentsResponse = await response.Content
                .ReadFromJsonAsync<MicrosoftFoundryDeploymentsResponse>(cancellationToken);

            return MapDeployments(deploymentsResponse);
        }
        catch (Exception ex) when (ex is HttpRequestException or Azure.Identity.AuthenticationFailedException)
        {
            _logger.LogWarning(ex, "Failed to fetch deployments from API.");
            return [];
        }
    }

    /// <summary>
    /// Projects a deployments API response into the listing entries the capabilities consume.
    /// </summary>
    /// <remarks>
    /// Separate from the fetch so it can be exercised without a live Foundry resource: everything above it
    /// in <see cref="FetchDeploymentsFromApiAsync"/> is Entra ID token acquisition and HTTP.
    /// </remarks>
    internal static IReadOnlyList<MicrosoftFoundryModelInfo> MapDeployments(
        MicrosoftFoundryDeploymentsResponse? response)
    {
        if (response?.Value is null || response.Value.Count == 0)
        {
            return [];
        }

        return response.Value
            .Select(d => new MicrosoftFoundryModelInfo
            {
                // Use the deployment name as the model ID (this is what gets passed to the API)
                Id = d.Name,

                // What the deployment actually fronts. A deployment name is user-chosen and can say
                // nothing about the model behind it, so this is the only reliable basis for deciding
                // which settings the model accepts, and for labelling it in the model picker.
                ModelName = d.ModelName,
                ModelVersion = d.ModelVersion,
                ModelPublisher = d.ModelPublisher,
            })
            .OrderBy(m => m.Id)
            .ToList();
    }

    private async Task<IReadOnlyList<MicrosoftFoundryModelInfo>> FetchModelsFromApiAsync(
        MicrosoftFoundryProviderSettings settings,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();

        // Build the models endpoint URL
        var baseEndpoint = settings.Endpoint!.TrimEnd('/');
        var modelsUrl = $"{baseEndpoint}/openai/models?api-version={ApiVersion}";

        using var request = new HttpRequestMessage(HttpMethod.Get, modelsUrl);

        // Use bearer token for Entra ID auth, api-key header for API key auth
        if (HasEntraIdCredentials(settings))
        {
            var tokenCredential = BuildTokenCredential(settings);
            var tokenRequestContext = new Azure.Core.TokenRequestContext([CognitiveServicesScope]);
            var accessToken = await tokenCredential.GetTokenAsync(tokenRequestContext, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
        }
        else
        {
            request.Headers.Add("api-key", settings.ApiKey);
        }

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Models API ({Url}) returned {StatusCode}.",
                    modelsUrl,
                    (int)response.StatusCode);
                return [];
            }

            var modelsResponse = await response.Content
                .ReadFromJsonAsync<MicrosoftFoundryModelsResponse>(cancellationToken);

            if (modelsResponse?.Data is null)
            {
                return [];
            }

            return modelsResponse.Data
                .Where(m => m.Status == "succeeded" || m.Status is null)
                .OrderBy(m => m.Id)
                .ToList();
        }
        catch (HttpRequestException)
        {
            // If the API call fails, return empty list - user can still type model names manually
            return [];
        }
    }

    private static void ValidateSettings(MicrosoftFoundryProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new InvalidOperationException("Microsoft AI Foundry endpoint is required.");
        }

        if (!HasApiKeyCredentials(settings) && !HasEntraIdCredentials(settings))
        {
            throw new InvalidOperationException(
                "Microsoft AI Foundry requires either an API key or Entra ID credentials (TenantId, ClientId, ClientSecret).");
        }
    }

    private static string GetCacheKey(MicrosoftFoundryProviderSettings settings)
    {
        if (HasEntraIdCredentials(settings))
        {
            // Cache per tenant + client + endpoint combination for Entra ID auth
            return $"{CacheKeyPrefix}entra:{settings.ProjectName}:{settings.TenantId}:{settings.ClientId}:{settings.Endpoint}";
        }

        // Cache per API key + endpoint combination
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{settings.Endpoint}";
    }
}
