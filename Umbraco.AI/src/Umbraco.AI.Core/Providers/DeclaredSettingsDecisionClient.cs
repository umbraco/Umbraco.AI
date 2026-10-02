using Umbraco.AI.Core.Decision;

#pragma warning disable UMBRACOAI_DECISION // IAIDecisionClient is experimental

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Decision client decorator that removes the core request options the capability declares the target
/// model does not accept, before delegating to the inner client.
/// </summary>
/// <remarks>
/// The decision counterpart of <see cref="DeclaredSettingsSpeechToTextClient"/>. Unlike its siblings,
/// <see cref="AIDecisionOptions"/> currently exposes no strippable per-request settings (no shipped
/// provider declares any sampling-style knobs yet), so <see cref="Filter"/> is a pass-through today. It
/// still exists so
/// <see cref="AIDecisionCapabilityBase{TSettings}"/> wraps every client it builds in the same seam every
/// other capability base does, ready for the day <see cref="AIDecisionOptions"/> grows a field a provider
/// can decline — at which point this needs the capability/boundModelId/logger the other DeclaredSettings*
/// clients take, added back alongside that first real strippable field.
/// </remarks>
internal sealed class DeclaredSettingsDecisionClient(IAIDecisionClient innerClient) : IAIDecisionClient
{
    /// <inheritdoc />
    public Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        => innerClient.AskAsync(question, Filter(options), cancellationToken);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => innerClient.GetService(serviceType, serviceKey);

    /// <inheritdoc />
    public void Dispose() => innerClient.Dispose();

    private static AIDecisionOptions? Filter(AIDecisionOptions? options)
    {
        // Nothing on AIDecisionOptions is strippable yet — see remarks above.
        return options;
    }
}
