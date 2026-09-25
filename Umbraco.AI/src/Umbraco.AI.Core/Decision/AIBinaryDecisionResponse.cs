using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>The answer to an <see cref="AIBinaryDecisionQuestion"/>.</summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIBinaryDecisionResponse : AIDecisionResponse
{
    /// <summary>The model's estimated probability that the answer is "yes", from 0.0 to 1.0.</summary>
    public required double Probability { get; init; }

    /// <summary>The yes/no answer, derived from <see cref="Probability"/> (true when it is at least 0.5).</summary>
    public bool Answer => Probability >= 0.5;

    /// <inheritdoc />
    public override double Confidence => Answer ? Probability : 1 - Probability;
}
