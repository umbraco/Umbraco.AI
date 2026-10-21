#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability

using Microsoft.Extensions.Caching.Memory;
using Moq;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Cloudflare.Tests.Unit.Fakes;

/// <summary>
/// Builds a real <see cref="CloudflareProvider"/> and gets clients through its real capability, with only the
/// <see cref="HttpMessageHandler"/> faked. Copies <c>TypeSafeTestHost</c>.
/// </summary>
internal static class CloudflareTestHost
{
    public const string AccountId = "test-account";

    public const string ApiToken = "test-api-token";

    public const string DefaultEndpoint = "https://api.cloudflare.com/client/v4";

    public const string ModelId = "@cf/cloudflare/clairic";

    /// <summary>A successful model-search reply, used by the connection check.</summary>
    public const string ModelSearchAnswer =
        """{"result":[{"name":"@cf/cloudflare/clairic","task":{"name":"Text Generation"}}],"success":true,"errors":[],"messages":[]}""";

    /// <summary>Settings with no Endpoint set, so every request proves the default endpoint is used.</summary>
    public static CloudflareProviderSettings Settings(string? apiToken = ApiToken)
        => new() { AccountId = AccountId, ApiToken = apiToken };

    public static CloudflareProvider CreateProvider(HttpMessageHandler? handler = null, IMemoryCache? cache = null)
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler ?? new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(ModelSearchAnswer))));

        return new CloudflareProvider(new ActivatorProviderInfrastructure(), httpClientFactory.Object, cache ?? new MemoryCache(new MemoryCacheOptions()));
    }

    public static IAIExtractionCapability CreateCapability(HttpMessageHandler? handler = null, IMemoryCache? cache = null)
        => (IAIExtractionCapability)CreateProvider(handler, cache).GetCapability<CloudflareExtractionCapability>();

    public static async Task<IAIExtractionClient> CreateClientAsync(HttpMessageHandler handler, string? modelId = ModelId)
        => await CreateCapability(handler).CreateClientAsync(Settings(), modelId, default);

    private sealed class ActivatorProviderInfrastructure : IAIProviderInfrastructure
    {
        public IAICapabilityFactory CapabilityFactory { get; } = new ActivatorCapabilityFactory();

        public IAIEditableModelSchemaBuilder SchemaBuilder { get; } = Mock.Of<IAIEditableModelSchemaBuilder>();

        private sealed class ActivatorCapabilityFactory : IAICapabilityFactory
        {
            public TCapability Create<TCapability>(IAIProvider provider)
                where TCapability : class, IAICapability
                => (TCapability)Activator.CreateInstance(typeof(TCapability), provider)!;
        }
    }
}
