using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A client capable of answering a batch of typed <see cref="AIDecisionQuestion"/>s — yes/no, a choice
/// from a fixed set, or a numeric score — about the same shared content in a single model call.
/// </summary>
/// <remarks>
/// Unlike every other client type in this codebase, <see cref="IAIDecisionClient"/> is not a
/// Microsoft.Extensions.AI type Umbraco AI is merely wrapping — no M.E.AI abstraction exists for this
/// shape of model, so this is a proprietary, Umbraco-AI-owned client abstraction. See the
/// decision-capability architecture doc for why, and for what would replace this if M.E.AI ever ships
/// an official equivalent. The method is named <see cref="GetResponseAsync"/>, matching the name M.E.AI's
/// own design uses, so that later swap is mechanical.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public interface IAIDecisionClient : IDisposable
{
    /// <summary>
    /// Answers every question in <paramref name="request"/> in a single model call.
    /// </summary>
    /// <param name="request">The shared state and questions to ask.</param>
    /// <param name="options">Optional per-call overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AIDecisionResponse> GetResponseAsync(
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mirrors <c>IChatClient.GetService</c> — lets tracking/telemetry middleware and callers reach
    /// through wrapper layers the same way every other client does.
    /// </summary>
    /// <param name="serviceType">The type of service to resolve.</param>
    /// <param name="serviceKey">An optional key to disambiguate multiple services of the same type.</param>
    object? GetService(Type serviceType, object? serviceKey = null);
}
