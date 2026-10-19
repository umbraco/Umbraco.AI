using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A batch of one or more typed <see cref="AIDecisionQuestion"/>s about the same shared content,
/// answered by an <see cref="IAIDecisionClient"/> in a single model call.
/// </summary>
/// <remarks>
/// Mirrors the shape Microsoft is converging on for M.E.AI's own decision abstraction (see the
/// decision-capability architecture doc): shared state, heterogeneous id'd questions, keyed answers.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIDecisionRequest
{
    /// <summary>The content being judged, shared by every question in <see cref="Questions"/>.</summary>
    public string? State { get; init; }

    /// <summary>
    /// The questions to ask, 1..n. Every question's <see cref="AIDecisionQuestion.Id"/> must be
    /// non-blank and unique within the request — enforced by <see cref="ValidatingDecisionClient"/>
    /// before any provider is reached.
    /// </summary>
    public required IReadOnlyList<AIDecisionQuestion> Questions { get; init; }
}
