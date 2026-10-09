using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>An <see cref="AIDecisionQuestion"/> answered with a numeric score against labelled levels.</summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIScoreDecisionQuestion : AIDecisionQuestion<AIScoreDecisionAnswer>
{
    /// <summary>
    /// The score's levels, lowest first; list position is the level index. Must contain between 2 and
    /// 10 entries with non-blank descriptions — enforced by <see cref="ValidatingDecisionClient"/>
    /// before any provider is reached.
    /// </summary>
    public required IReadOnlyList<AIDecisionScoreLevel> Levels { get; init; }
}
