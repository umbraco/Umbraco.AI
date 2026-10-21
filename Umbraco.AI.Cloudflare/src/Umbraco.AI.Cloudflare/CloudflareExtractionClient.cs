#pragma warning disable UMBRACOAI_EXTRACTION // Implements the experimental Umbraco.AI extraction client contract

using Umbraco.AI.Core.Extraction;

namespace Umbraco.AI.Cloudflare;

/// <summary>
/// Extraction client for Cloudflare's Clairic model. The wire mapping is not implemented yet.
/// </summary>
internal sealed class CloudflareExtractionClient(HttpClient httpClient, CloudflareProviderSettings settings, string modelId)
    : IAIExtractionClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly CloudflareProviderSettings _settings = settings;
    private readonly string _modelId = modelId;

    /// <inheritdoc />
    public Task<AIExtractionResponse> GetResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("The Cloudflare extraction wire mapping is not implemented yet.");

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    /// <inheritdoc />
    public void Dispose() => _httpClient.Dispose();
}
