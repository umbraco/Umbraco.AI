using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// A client capable of reading a batch of typed <see cref="AIExtractionField"/>s — text, numbers,
/// dates, times, durations or lists — out of the same shared text in a single model call.
/// </summary>
/// <remarks>
/// Like <see cref="Decision.IAIDecisionClient"/>, this is not a Microsoft.Extensions.AI type Umbraco AI
/// is merely wrapping — no M.E.AI abstraction exists for this shape of model, so this is a proprietary,
/// Umbraco-AI-owned client abstraction. The method is named <see cref="GetResponseAsync"/>, matching the
/// name M.E.AI's own clients use, so a later swap is mechanical. Providers return already-typed values;
/// turning a provider's wire strings into <see cref="DateOnly"/>, <see cref="decimal"/> and the like is
/// the provider's job, not Core's.
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public interface IAIExtractionClient : IDisposable
{
    /// <summary>
    /// Extracts every field in <paramref name="request"/> in a single model call.
    /// </summary>
    /// <param name="request">The shared text and fields to extract.</param>
    /// <param name="options">Optional per-call overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AIExtractionResponse> GetResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mirrors <c>IChatClient.GetService</c> — lets tracking/telemetry middleware and callers reach
    /// through wrapper layers the same way every other client does.
    /// </summary>
    /// <param name="serviceType">The type of service to resolve.</param>
    /// <param name="serviceKey">An optional key to disambiguate multiple services of the same type.</param>
    object? GetService(Type serviceType, object? serviceKey = null);
}
