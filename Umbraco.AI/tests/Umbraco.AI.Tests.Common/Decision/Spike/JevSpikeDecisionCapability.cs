#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Tests.Common.Decision.Spike;

/// <summary>
/// The <see cref="AICapability.Decision"/> capability for <see cref="JevSpikeProvider"/>. Builds a
/// fresh <see cref="JevSpikeDecisionClient"/> per request, the same lifecycle other capabilities'
/// <c>CreateClient</c> overrides use for their own SDK clients (e.g. a fresh <c>OpenAIClient</c> per
/// call). The wrapped <see cref="HttpClient"/> is not fresh in the same sense, though — it comes from
/// <see cref="JevSpikeProvider.CreateHttpClient"/> (<see cref="IHttpClientFactory"/>-backed, mirroring
/// <c>FireworksAIProvider</c>/<c>OpenRouterProvider</c>), so its handler is pooled and reused across
/// calls rather than opening a fresh socket each time. <see cref="JevSpikeDecisionClient"/> must not
/// dispose it; the factory owns its lifetime.
/// </summary>
public sealed class JevSpikeDecisionCapability(JevSpikeProvider provider)
    : AIDecisionCapabilityBase<JevSpikeProviderSettings>(provider)
{
    private new JevSpikeProvider Provider => (JevSpikeProvider)base.Provider;

    /// <summary>
    /// Jev exposes no model catalog to discover (its whole pitch is skipping token-by-token,
    /// per-model generation) — a single placeholder descriptor is enough for profile creation.
    /// </summary>
    protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        JevSpikeProviderSettings settings,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AIModelDescriptor>>(
            [new AIModelDescriptor(new AIModelRef(Provider.Id, "default"), "Default")]);

    /// <inheritdoc />
    protected override IAIDecisionClient CreateClient(JevSpikeProviderSettings settings, string? modelId)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Jev API key is required.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl) ? "https://api.typesafe.ai" : settings.BaseUrl;
        var answerPath = string.IsNullOrWhiteSpace(settings.AnswerPath) ? "/v1/answer" : settings.AnswerPath;

        var httpClient = Provider.CreateHttpClient();
        httpClient.BaseAddress = new Uri(baseUrl);

        return new JevSpikeDecisionClient(httpClient, settings.ApiKey, answerPath);
    }
}
