using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>The answer to an <see cref="AIScoreDecisionQuestion"/>.</summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIScoreDecisionResponse : AIDecisionResponse
{
    /// <summary>The 0-based, fractional index into <see cref="AIScoreDecisionQuestion.Levels"/> the model landed on.</summary>
    public required double Score { get; init; }

    /// <summary>The label of the level nearest to <see cref="Score"/>.</summary>
    public required string Level { get; init; }

    /// <summary>The model's confidence in <see cref="Score"/>, from 0.0 to 1.0.</summary>
    public required double ScoreConfidence { get; init; }

    /// <summary>The model's estimated probability for each level, keyed by label, when reported.</summary>
    public IReadOnlyDictionary<string, double> Probabilities { get; init; } = new Dictionary<string, double>();

    /// <inheritdoc />
    public override double Confidence => ScoreConfidence;
}
