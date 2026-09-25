using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>An <see cref="AIDecisionQuestion"/> answered with a numeric score against labelled levels.</summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIScoreDecisionQuestion : AIDecisionQuestion<AIScoreDecisionResponse>
{
    /// <summary>
    /// The score's labels, lowest first. Must contain between 2 and 10 non-blank entries — enforced by
    /// <see cref="ValidatingDecisionClient"/> before any provider is reached.
    /// </summary>
    public required IReadOnlyList<string> Levels { get; init; }
}
