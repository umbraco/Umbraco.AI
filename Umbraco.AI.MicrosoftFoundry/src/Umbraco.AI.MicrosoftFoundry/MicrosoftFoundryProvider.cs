using System.Net.Http.Json;
using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// AI provider for Microsoft AI Foundry (Azure AI Inference).
/// </summary>
/// <remarks>
/// This provider supports all models available through Microsoft AI Foundry's unified endpoint,
/// including OpenAI models (GPT-4, GPT-4o), Mistral, Llama, Cohere, Phi, and more.
/// One endpoint and API key provides access to all deployed models.
/// </remarks>
[AIProvider("microsoft-foundry", "Microsoft AI Foundry")]
public class MicrosoftFoundryProvider : AIProviderBase<MicrosoftFoundryProviderSettings>
{
    private const string CacheKeyPrefix = "MicrosoftFoundry_Models_";
    private const string ApiVersion = "2024-10-21";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftFoundryProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public MicrosoftFoundryProvider(
        IAiProviderInfrastructure infrastructure,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory)
        : base(infrastructure)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
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

        var models = await FetchModelsFromApiAsync(settings, cancellationToken);

        _cache.Set(cacheKey, models, CacheDuration);

        return models;
    }

    /// <summary>
    /// Creates a ChatCompletionsClient configured with the provided settings.
    /// </summary>
    /// <param name="settings">The provider settings.</param>
    /// <param name="modelId">The model/deployment name to use.</param>
    /// <returns>A configured ChatCompletionsClient.</returns>
    internal static ChatCompletionsClient CreateChatCompletionsClient(MicrosoftFoundryProviderSettings settings, string modelId)
    {
        ValidateSettings(settings);

        var endpoint = BuildModelEndpoint(settings.Endpoint!, modelId);
        var credential = new AzureKeyCredential(settings.ApiKey!);

        // For Azure OpenAI endpoints, we need to add the api-key header via policy
        var options = new AzureAIInferenceClientOptions();
        options.AddPolicy(new AddApiKeyHeaderPolicy(settings.ApiKey!), HttpPipelinePosition.PerCall);

        return new ChatCompletionsClient(endpoint, credential, options);
    }

    /// <summary>
    /// Creates an EmbeddingsClient configured with the provided settings.
    /// </summary>
    /// <param name="settings">The provider settings.</param>
    /// <param name="modelId">The model/deployment name to use.</param>
    /// <returns>A configured EmbeddingsClient.</returns>
    internal static EmbeddingsClient CreateEmbeddingsClient(MicrosoftFoundryProviderSettings settings, string modelId)
    {
        ValidateSettings(settings);

        var endpoint = BuildModelEndpoint(settings.Endpoint!, modelId);
        var credential = new AzureKeyCredential(settings.ApiKey!);

        // For Azure OpenAI endpoints, we need to add the api-key header via policy
        var options = new AzureAIInferenceClientOptions();
        options.AddPolicy(new AddApiKeyHeaderPolicy(settings.ApiKey!), HttpPipelinePosition.PerCall);

        return new EmbeddingsClient(endpoint, credential, options);
    }

    /// <summary>
    /// Builds the full endpoint URL including the model/deployment path.
    /// </summary>
    /// <remarks>
    /// Azure OpenAI endpoints require: https://{resource}.openai.azure.com/openai/deployments/{model}
    /// Microsoft AI Foundry endpoints require: https://{resource}.services.ai.azure.com/models
    /// </remarks>
    private static Uri BuildModelEndpoint(string baseEndpoint, string modelId)
    {
        var endpoint = baseEndpoint.TrimEnd('/');

        // Check if this is an Azure OpenAI endpoint
        if (endpoint.Contains(".openai.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            // Azure OpenAI requires the deployment in the path
            return new Uri($"{endpoint}/openai/deployments/{modelId}");
        }

        // Check if this is a Microsoft AI Foundry endpoint
        if (endpoint.Contains(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            // AI Foundry requires /models path, model name is passed in request body
            if (!endpoint.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
            {
                return new Uri($"{endpoint}/models");
            }
        }

        return new Uri(endpoint);
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
        request.Headers.Add("api-key", settings.ApiKey);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // If models endpoint doesn't work, return empty list (user will type model name)
                return [];
            }

            var modelsResponse = await response.Content
                .ReadFromJsonAsync<MicrosoftFoundryModelsResponse>(cancellationToken)
                ;

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

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Microsoft AI Foundry API key is required.");
        }
    }

    private static string GetCacheKey(MicrosoftFoundryProviderSettings settings)
    {
        // Cache per API key + endpoint combination
        return $"{CacheKeyPrefix}{settings.ApiKey?.GetHashCode()}:{settings.Endpoint}";
    }

    /// <summary>
    /// HTTP pipeline policy to add the api-key header for Azure OpenAI authentication.
    /// </summary>
    private sealed class AddApiKeyHeaderPolicy(string apiKey) : HttpPipelinePolicy
    {
        public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            message.Request.Headers.SetValue("api-key", apiKey);
            ProcessNext(message, pipeline);
        }

        public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            message.Request.Headers.SetValue("api-key", apiKey);
            return ProcessNextAsync(message, pipeline);
        }
    }
}
