#pragma warning disable UMBRACOAI_EXTRACTION // Implements the experimental Umbraco.AI extraction capability

using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Cloudflare;

/// <summary>
/// AI extraction capability for Cloudflare's Clairic model.
/// </summary>
/// <remarks>
/// Experimental, gated by the <c>Umbraco:AI:Experimental:Extraction</c> feature flag and the
/// <c>UMBRACOAI_EXTRACTION</c> diagnostic. Cloudflare's model search cannot tell Clairic apart from other
/// models, so the model list is a static array rather than a live lookup.
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public class CloudflareExtractionCapability(CloudflareProvider provider)
    : AIExtractionCapabilityBase<CloudflareProviderSettings>(provider)
{
    private const string DefaultModel = "@cf/cloudflare/clairic";

    private static readonly IReadOnlyList<AIModelDescriptor> Models =
    [
        new AIModelDescriptor(new AIModelRef("cloudflare", DefaultModel), "Clairic"),
    ];

    private new CloudflareProvider Provider => (CloudflareProvider)base.Provider;

    /// <inheritdoc />
    protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        CloudflareProviderSettings settings,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Models);

    /// <inheritdoc />
    /// <remarks>
    /// Validates the account ID and token up front so a missing value fails before any network call.
    /// </remarks>
    protected override Task<IAIExtractionClient> CreateClientAsync(
        CloudflareProviderSettings settings,
        string? modelId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.AccountId))
        {
            throw new InvalidOperationException("Cloudflare account ID is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.ApiToken))
        {
            throw new InvalidOperationException("Cloudflare API token is required.");
        }

        IAIExtractionClient client = new CloudflareExtractionClient(
            Provider.CreateHttpClient(),
            settings,
            modelId ?? DefaultModel);

        return Task.FromResult(client);
    }
}
