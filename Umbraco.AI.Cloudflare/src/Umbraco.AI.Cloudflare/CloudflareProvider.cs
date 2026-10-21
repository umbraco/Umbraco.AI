#pragma warning disable UMBRACOAI_EXTRACTION // Registers the experimental extraction capability

using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Cloudflare;

/// <summary>
/// AI provider for Cloudflare Workers AI, currently exposing only the experimental Extraction capability.
/// </summary>
/// <remarks>
/// Calls Cloudflare directly over HTTP (<see cref="IHttpClientFactory"/> + <c>System.Text.Json</c>), like
/// <c>Umbraco.AI.TypeSafe</c>. Named after the company so further Cloudflare capabilities can be added to
/// this package later.
/// </remarks>
[AIProvider("cloudflare", "Cloudflare")]
public class CloudflareProvider : AIProviderBase<CloudflareProviderSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudflareProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="httpClientFactory">Used to create the <see cref="HttpClient"/> the extraction client sends requests with.</param>
    /// <param name="cache">Caches a successful connection check.</param>
    public CloudflareProvider(IAIProviderInfrastructure infrastructure, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
        Cache = cache;

        WithCapability<CloudflareExtractionCapability>();
    }

    /// <summary>
    /// The cache used for connection checks.
    /// </summary>
    internal IMemoryCache Cache { get; }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> for calling the Cloudflare API. It carries no base address or
    /// auth header; each request is built from <see cref="CloudflareProviderSettings"/>.
    /// </summary>
    internal HttpClient CreateHttpClient() => _httpClientFactory.CreateClient(nameof(CloudflareProvider));
}
