using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>The answer to an <see cref="AIScoreDecisionQuestion"/>.</summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIScoreDecisionAnswer : AIDecisionAnswer
{
    /// <summary>The 0-based, fractional index into <see cref="AIScoreDecisionQuestion.Levels"/> the
    /// model landed on (the expected level index).</summary>
    public required double Score { get; init; }

    /// <summary>The model's estimated probability for every level, keyed by its 0-based index into
    /// <see cref="AIScoreDecisionQuestion.Levels"/>.</summary>
    public required IReadOnlyDictionary<int, double> Probabilities { get; init; }

    /// <summary>The provider's own confidence summary, from 0.0 to 1.0, when reported. Not comparable
    /// across providers.</summary>
    public double? Confidence { get; init; }
}
