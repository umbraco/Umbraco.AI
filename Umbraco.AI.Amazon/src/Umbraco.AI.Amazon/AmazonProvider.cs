using Amazon;
using Amazon.Bedrock;
using Amazon.Bedrock.Model;
using Amazon.BedrockRuntime;
using Amazon.Runtime;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Amazon;

/// <summary>
/// AI provider for Amazon Bedrock services.
/// </summary>
[AIProvider("amazon", "Amazon Bedrock")]
public class AmazonProvider : AIProviderBase<AmazonProviderSettings>
{
    private const string CacheKeyPrefix = "Amazon_Models_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="cache">The memory cache.</param>
    public AmazonProvider(IAIProviderInfrastructure infrastructure, IMemoryCache cache)
        : base(infrastructure)
    {
        _cache = cache;

        WithCapability<AmazonChatCapability>();
        WithCapability<AmazonEmbeddingCapability>();
    }

    /// <summary>
    /// Gets all available inference profile IDs from the Bedrock API with caching.
    /// Uses ListInferenceProfiles to get cross-region inference profile IDs that support on-demand invocation.
    /// </summary>
    /// <param name="settings">The provider settings containing AWS credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all available inference profile IDs.</returns>
    internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(
        AmazonProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        ValidateSettings(settings);

        var cacheKey = GetCacheKey(settings);

        if (_cache.TryGetValue<IReadOnlyList<string>>(cacheKey, out var cachedModels) && cachedModels is not null)
        {
            return cachedModels;
        }

        using var client = CreateBedrockClient(settings);

        // Use ListInferenceProfiles to get cross-region inference profiles
        // These are the IDs that actually work for on-demand invocation (e.g., eu.amazon.nova-lite-v1:0)
        var request = new ListInferenceProfilesRequest
        {
            MaxResults = 1000
        };

        var allProfiles = new List<string>();
        string? nextToken = null;

        do
        {
            request.NextToken = nextToken;
            var response = await client.ListInferenceProfilesAsync(request, cancellationToken);

            allProfiles.AddRange(response.InferenceProfileSummaries.Select(p => p.InferenceProfileId));
            nextToken = response.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));

        var modelIds = allProfiles
            .OrderBy(id => id)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)modelIds, CacheDuration);

        return modelIds;
    }

    /// <summary>
    /// Creates an Amazon Bedrock client configured with the provided settings.
    /// </summary>
    internal static AmazonBedrockClient CreateBedrockClient(AmazonProviderSettings settings)
    {
        ValidateSettings(settings);

        var credentials = new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey);
        var region = RegionEndpoint.GetBySystemName(settings.Region);

        if (!string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            var config = new AmazonBedrockConfig
            {
                RegionEndpoint = region,
                ServiceURL = settings.Endpoint
            };
            return new AmazonBedrockClient(credentials, config);
        }

        return new AmazonBedrockClient(credentials, region);
    }

    /// <summary>
    /// Creates an Amazon Bedrock Runtime client configured with the provided settings.
    /// </summary>
    internal static AmazonBedrockRuntimeClient CreateBedrockRuntimeClient(AmazonProviderSettings settings)
    {
        ValidateSettings(settings);

        var credentials = new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey);
        var region = RegionEndpoint.GetBySystemName(settings.Region);

        if (!string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            var config = new AmazonBedrockRuntimeConfig
            {
                RegionEndpoint = region,
                ServiceURL = settings.Endpoint
            };
            return new AmazonBedrockRuntimeClient(credentials, config);
        }

        return new AmazonBedrockRuntimeClient(credentials, region);
    }

    private static void ValidateSettings(AmazonProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Region))
        {
            throw new InvalidOperationException("AWS region is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.AccessKeyId))
        {
            throw new InvalidOperationException("AWS Access Key ID is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.SecretAccessKey))
        {
            throw new InvalidOperationException("AWS Secret Access Key is required.");
        }
    }

    private static string GetCacheKey(AmazonProviderSettings settings)
    {
        // Cache per credentials + region + endpoint combination
        var endpoint = settings.Endpoint ?? "default";
        return $"{CacheKeyPrefix}{settings.AccessKeyId?.GetHashCode()}:{settings.Region}:{endpoint}";
    }
}
