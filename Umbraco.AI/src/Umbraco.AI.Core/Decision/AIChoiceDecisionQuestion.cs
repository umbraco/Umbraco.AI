using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>An <see cref="AIDecisionQuestion"/> answered by selecting one of a fixed set of options.</summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIChoiceDecisionQuestion : AIDecisionQuestion<AIChoiceDecisionResponse>
{
    /// <summary>
    /// The options to choose from. Must contain between 2 and 255 entries with unique, non-blank keys —
    /// enforced by <see cref="ValidatingDecisionClient"/> before any provider is reached.
    /// </summary>
    public required IReadOnlyList<AIDecisionOption> Options { get; init; }
}
