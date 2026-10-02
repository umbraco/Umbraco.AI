using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A client capable of answering a typed <see cref="AIDecisionQuestion"/> — yes/no, a choice from a
/// fixed set, or a numeric score.
/// </summary>
/// <remarks>
/// Unlike every other client type in this codebase, <see cref="IAIDecisionClient"/> is not a
/// Microsoft.Extensions.AI type Umbraco AI is merely wrapping — no M.E.AI abstraction exists for this
/// shape of model, so this is a proprietary, Umbraco-AI-owned client abstraction. See the
/// decision-capability architecture doc for why, and for what would replace this if M.E.AI ever ships
/// an official equivalent.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public interface IAIDecisionClient : IDisposable
{
    /// <summary>
    /// Asks the model a typed question and returns a correspondingly typed answer.
    /// </summary>
    /// <param name="question">The question to ask, including the expected answer shape.</param>
    /// <param name="options">Optional per-call overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
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
