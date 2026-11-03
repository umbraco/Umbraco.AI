using Umbraco.AI.Core.Extraction;

#pragma warning disable UMBRACOAI_EXTRACTION // IAIExtractionClient is experimental

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Extraction client decorator that removes the core request options the capability declares the target
/// model does not accept, before delegating to the inner client.
/// </summary>
/// <remarks>
/// The extraction counterpart of <see cref="DeclaredSettingsSpeechToTextClient"/>. Unlike its siblings,
/// <see cref="AIExtractionOptions"/> currently exposes no strippable per-request settings (no shipped
/// provider declares any such knobs yet), so <see cref="Filter"/> is a pass-through today. It
/// still exists so
/// <see cref="AIExtractionCapabilityBase{TSettings}"/> wraps every client it builds in the same seam every
/// other capability base does, ready for the day <see cref="AIExtractionOptions"/> grows a field a provider
/// can decline — at which point this needs the capability/boundModelId/logger the other DeclaredSettings*
/// clients take, added back alongside that first real strippable field.
/// </remarks>
internal sealed class DeclaredSettingsExtractionClient(IAIExtractionClient innerClient) : IAIExtractionClient
{
    /// <inheritdoc />
    public Task<AIExtractionResponse> GetResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
        => innerClient.GetResponseAsync(request, Filter(options), cancellationToken);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => innerClient.GetService(serviceType, serviceKey);

    /// <inheritdoc />
    public void Dispose() => innerClient.Dispose();

    private static AIExtractionOptions? Filter(AIExtractionOptions? options)
    {
        // Nothing on AIExtractionOptions is strippable yet — see remarks above.
        return options;
    }
}
